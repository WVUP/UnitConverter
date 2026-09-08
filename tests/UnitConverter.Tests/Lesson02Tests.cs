using System.Globalization;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.Testing;

namespace UnitConverter.Tests;

public class Lesson02Tests
{
    [Fact]
    public void ConversionType_SupportsGetBinding()
    {
        Type modelType = GetConversionsPageModelType();

        PropertyInfo? property = modelType.GetProperty("ConversionType");

        Assert.NotNull(property);

        BindPropertyAttribute? attribute = property.GetCustomAttribute<BindPropertyAttribute>();

        Assert.NotNull(attribute);
        Assert.True(attribute.SupportsGet);
    }

    [Fact]
    public void Input_SupportsGetBinding()
    {
        Type modelType = GetConversionsPageModelType();

        PropertyInfo? property = modelType.GetProperty("Input");

        Assert.NotNull(property);

        BindPropertyAttribute? attribute = property.GetCustomAttribute<BindPropertyAttribute>();

        Assert.NotNull(attribute);
        Assert.True(attribute.SupportsGet);
    }

    [Theory]
    [InlineData("MilesToKilometers", "10", 16.09344)]
    [InlineData("KilometersToMiles", "10", 6.2137119224)]
    [InlineData("FahrenheitToCelsius", "32", 0)]
    [InlineData("CelsiusToFahrenheit", "100", 212)]
    [InlineData("PoundsToKilograms", "10", 4.5359237)]
    [InlineData("KilogramsToPounds", "10", 22.0462262)]
    public void OnGet_PerformsRequestedConversion(string conversionType, string input, double expected)
    {
        PageModel model = CreatePageModel();

        SetProperty(model, "ConversionType", conversionType);
        SetProperty(model, "Input", input);

        InvokeOnGet(model);

        string output = GetStringProperty(model, "Output");

        Assert.True(
            double.TryParse(
                output,
                NumberStyles.Float | NumberStyles.AllowThousands,
                CultureInfo.InvariantCulture,
                out double actual) ||
            double.TryParse(
                output,
                NumberStyles.Float | NumberStyles.AllowThousands,
                CultureInfo.CurrentCulture,
                out actual),
            $"Output '{output}' is not a valid number.");

        Assert.InRange(actual, expected - 0.0001, expected + 0.0001);
    }

    [Fact]
    public void OnGet_InvalidNumericInput_SetsErrorMessage()
    {
        PageModel model = CreatePageModel();

        SetProperty(model, "ConversionType", "MilesToKilometers");
        SetProperty(model, "Input", "not-a-number");

        InvokeOnGet(model);

        Assert.True(model.ViewData.ContainsKey("ErrorMessage"));

        string? message = model.ViewData["ErrorMessage"]?.ToString();

        Assert.False(string.IsNullOrWhiteSpace(message));
    }

    [Fact]
    public void OnGet_UnknownConversionType_SetsErrorMessage()
    {
        PageModel model = CreatePageModel();

        SetProperty(model, "ConversionType", "BananasToOranges");
        SetProperty(model, "Input", "10");

        InvokeOnGet(model);

        Assert.True(model.ViewData.ContainsKey("ErrorMessage"));

        string? message = model.ViewData["ErrorMessage"]?.ToString();

        Assert.False(string.IsNullOrWhiteSpace(message));
    }

    [Fact]
    public void OnGet_ValidConversion_DoesNotSetErrorMessage()
    {
        PageModel model = CreatePageModel();

        SetProperty(model, "ConversionType", "MilesToKilometers");
        SetProperty(model, "Input", "10");

        InvokeOnGet(model);

        Assert.False(model.ViewData.ContainsKey("ErrorMessage"));
    }

    [Fact]
    public async Task ConversionRoute_ReturnsSuccessStatusCode()
    {
        await using WebApplicationFactory<Program> application = new WebApplicationFactory<Program>();

        using HttpClient client = application.CreateClient();

        HttpResponseMessage response = await client.GetAsync(
            "/Conversions/MilesToKilometers/10", TestContext.Current.CancellationToken);

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task ConversionRoute_DisplaysConversionInformation()
    {
        await using WebApplicationFactory<Program> application = new WebApplicationFactory<Program>();

        using HttpClient client = application.CreateClient();

        HttpResponseMessage response =
            await client.GetAsync("/Conversions/MilesToKilometers/10", TestContext.Current.CancellationToken);

        response.EnsureSuccessStatusCode();

        string content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Contains("MilesToKilometers", content, StringComparison.OrdinalIgnoreCase);

        Assert.Contains("10", content);
    }

    private static PageModel CreatePageModel()
    {
        Type modelType = GetConversionsPageModelType();

        object? instance = Activator.CreateInstance(modelType);

        PageModel model = Assert.IsAssignableFrom<PageModel>(instance);

        model.PageContext = new PageContext
        {
            ViewData = new ViewDataDictionary(
                new EmptyModelMetadataProvider(),
                new ModelStateDictionary())
        };

        return model;
    }

    private static Type GetConversionsPageModelType()
    {
        List<Type> candidates = typeof(Program).Assembly
            .GetTypes()
            .Where(type =>
                typeof(PageModel).IsAssignableFrom(type) &&
                !type.IsAbstract &&
                type.GetProperty("Input") is not null &&
                type.GetProperty("Output") is not null &&
                type.GetProperty("ConversionType") is not null)
            .ToList();

        Assert.Single(candidates);

        return candidates[0];
    }

    private static void SetProperty(PageModel model, string propertyName, string value)
    {
        PropertyInfo? property = model.GetType().GetProperty(propertyName);

        Assert.NotNull(property);

        property.SetValue(model, value);
    }

    private static string GetStringProperty(PageModel model, string propertyName)
    {
        PropertyInfo? property = model.GetType().GetProperty(propertyName);

        Assert.NotNull(property);

        object? value = property.GetValue(model);

        return Assert.IsType<string>(value);
    }

    private static void InvokeOnGet(PageModel model)
    {
        MethodInfo? method = model.GetType().GetMethod(
            "OnGet",
            BindingFlags.Public | BindingFlags.Instance,
            binder: null,
            types: Type.EmptyTypes,
            modifiers: null);

        Assert.NotNull(method);

        try
        {
            method.Invoke(model, null);
        }
        catch (TargetInvocationException ex)
            when (ex.InnerException is not null)
        {
            throw ex.InnerException;
        }
    }
}
