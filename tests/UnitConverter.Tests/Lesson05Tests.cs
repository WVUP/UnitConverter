using System.Net;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace UnitConverter.Tests;

public class Lesson05Tests
{
    private static readonly string[] RequiredHandlers =
    [
        "MilesToKilometers",
        "KilometersToMiles",
        "FahrenheitToCelsius",
        "CelsiusToFahrenheit",
        "PoundsToKilograms",
        "KilogramsToPounds"
    ];

    [Fact]
    public void IConversionService_ExistsAndIsAnInterface()
    {
        var serviceType = GetTypeByName("IConversionService");
        Assert.True(serviceType.IsInterface, "IConversionService should be declared as an interface.");
    }

    [Fact]
    public void IConversionService_DefinesConvertMethod()
    {
        var serviceType = GetTypeByName("IConversionService");
        var method = serviceType.GetMethod("Convert");
        Assert.NotNull(method);

        var parameters = method.GetParameters();
        Assert.Equal(2, parameters.Length);
        Assert.Equal(typeof(decimal), parameters[0].ParameterType);
        Assert.Equal(typeof(string), parameters[1].ParameterType);
        Assert.Equal(typeof(decimal), method.ReturnType);
    }

    [Fact]
    public void UnitOfConversionService_ImplementsIConversionService()
    {
        var interfaceType = GetTypeByName("IConversionService");
        var implementationType = GetTypeByName("UnitOfConversionService");

        Assert.True(interfaceType.IsAssignableFrom(implementationType),
            "UnitOfConversionService should implement IConversionService.");
    }

    [Fact]
    public void ConversionTypes_IsInModelsNamespace()
    {
        var conversionTypes = GetTypeByName("ConversionTypes");

        Assert.Equal("UnitConverter.Models", conversionTypes.Namespace);
    }

    [Fact]
    public void QuickConversionsPageModel_RequiresIConversionService()
    {
        var interfaceType = GetTypeByName("IConversionService");
        var pageModelType = GetQuickConversionsPageModelType();

        var found = pageModelType.GetConstructors().Any(
            constructor => constructor.GetParameters().Any(
                parameter => parameter.ParameterType == interfaceType));

        Assert.True(found, "QuickConversions should receive IConversionService through constructor injection.");
    }

    [Fact]
    public void DependencyInjection_ResolvesUnitOfConversionService()
    {
        using var application = new WebApplicationFactory<Program>();
        var interfaceType = GetTypeByName("IConversionService");
        var implementationType = GetTypeByName("UnitOfConversionService");

        var service = application.Services.GetService(interfaceType);

        Assert.NotNull(service);
        Assert.Equal(implementationType, service.GetType());
    }

    [Fact]
    public void DependencyInjection_UsesSingletonLifetime()
    {
        using var application = new WebApplicationFactory<Program>();
        var interfaceType = GetTypeByName("IConversionService");

        using var scope1 = application.Services.CreateScope();
        using var scope2 = application.Services.CreateScope();

        var service1 = scope1.ServiceProvider.GetRequiredService(interfaceType);
        var service2 = scope2.ServiceProvider.GetRequiredService(interfaceType);

        Assert.Same(service1, service2);
    }

    [Theory]
    [InlineData("MilesToKilometers", "10", "16.09")]
    [InlineData("KilometersToMiles", "10", "6.21")]
    [InlineData("FahrenheitToCelsius", "212", "100")]
    [InlineData("CelsiusToFahrenheit", "100", "212")]
    [InlineData("PoundsToKilograms", "10", "4.53")]
    [InlineData("KilogramsToPounds", "10", "22.04")]
    public async Task QuickConversions_DisplaysConversionResult(
        string handler, string input, string expected)
    {
        await using var application = new WebApplicationFactory<Program>();
        using var client = application.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync($"/QuickConversions?handler={handler}&input={input}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Contains(expected, content);
    }

    [Fact]
    public async Task QuickConversions_InvalidInputDisplaysAccessibleError()
    {
        await using var application = new WebApplicationFactory<Program>();
        using var client = application.CreateClient(
			new WebApplicationFactoryClientOptions { AllowAutoRedirect = false }
		);

        var response = await client.GetAsync(
            "/QuickConversions?handler=MilesToKilometers&input=not-a-number",
            TestContext.Current.CancellationToken
		);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Contains("role=\"alert\"", content, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [MemberData(nameof(HandlerNames))]
    public void QuickConversions_RetainsRequiredNamedHandlers(string handler)
    {
        var pageModelType = GetQuickConversionsPageModelType();
        Assert.NotNull(pageModelType.GetMethod($"OnGet{handler}"));
    }

    private static IReadOnlyDictionary<string, string> GetConversionTypes()
    {
        var conversionTypesType = GetTypeByName("ConversionTypes");

        var allField = conversionTypesType.GetField("All",BindingFlags.Public | BindingFlags.Static);

        Assert.NotNull(allField);

        return Assert.IsAssignableFrom<IReadOnlyDictionary<string, string>>(allField.GetValue(null));
    }

    public static IEnumerable<object[]> HandlerNames =>
        GetConversionTypes().Keys
            .Select(conversionType => new object[] { conversionType });

    private static Type GetTypeByName(string name)
    {
        var candidates = typeof(Program).Assembly.GetTypes()
            .Where(type => type.Name == name)
            .ToList();

        Assert.True(candidates.Count == 1,
            $"Expected exactly one type named {name}, but found {candidates.Count}.");

        return candidates[0];
    }

    private static Type GetQuickConversionsPageModelType()
    {
        var conversionTypes = GetConversionTypes();

        var candidates = typeof(Program).Assembly
            .GetTypes()
            .Where(type =>
                typeof(PageModel).IsAssignableFrom(type) &&
                !type.IsAbstract &&
                conversionTypes.Keys.All(conversionType =>
                    type.GetMethod($"OnGet{conversionType}") is not null))
            .ToList();

        Assert.Single(candidates);

        return candidates[0];
    }
}
