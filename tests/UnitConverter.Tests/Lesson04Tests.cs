using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Testing;

namespace UnitConverter.Tests;

public class Lesson04Tests
{
    [Fact]
    public async Task QuickConversionsPage_IsAvailable()
    {
        await using var application = new WebApplicationFactory<Program>();
        using var client = application.CreateClient();

        var response = await client.GetAsync("/QuickConversions", TestContext.Current.CancellationToken);

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task IndexPage_LinksToQuickConversions()
    {
        await using var application = new WebApplicationFactory<Program>();
        using var client = application.CreateClient();

        var response = await client.GetAsync("/", TestContext.Current.CancellationToken);

        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Contains("href=\"/QuickConversions\"", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task QuickConversionsPage_LinksBackToIndex()
    {
        await using var application = new WebApplicationFactory<Program>();
        using var client = application.CreateClient();

        var response = await client.GetAsync("/QuickConversions", TestContext.Current.CancellationToken);

        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Contains("href=\"/\"", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task QuickConversionsPage_ContainsRequiredFormControls()
    {
        await using var application = new WebApplicationFactory<Program>();
        using var client = application.CreateClient();

        var response = await client.GetAsync("/QuickConversions", TestContext.Current.CancellationToken);

        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Contains("type=\"text\"", content, StringComparison.OrdinalIgnoreCase);

        Assert.Contains("type=\"number\"", content, StringComparison.OrdinalIgnoreCase);

        Assert.Contains("<textarea", content, StringComparison.OrdinalIgnoreCase);

        Assert.Contains("type=\"range\"", content, StringComparison.OrdinalIgnoreCase);

        Assert.Contains("<select", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task QuickConversionsPage_UsesBootstrapFormClasses()
    {
        await using var application = new WebApplicationFactory<Program>();
        using var client = application.CreateClient();

        var response = await client.GetAsync("/QuickConversions", TestContext.Current.CancellationToken);

        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Contains("form-label", content, StringComparison.OrdinalIgnoreCase);

        Assert.Contains("form-control", content, StringComparison.OrdinalIgnoreCase);

        Assert.Contains("form-select", content, StringComparison.OrdinalIgnoreCase);

        Assert.Contains("btn-primary", content, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("MilesToKilometers")]
    [InlineData("KilometersToMiles")]
    [InlineData("FahrenheitToCelsius")]
    [InlineData("CelsiusToFahrenheit")]
    [InlineData("PoundsToKilograms")]
    [InlineData("KilogramsToPounds")]
    public void QuickConversionsPageModel_HasRequiredHandler(string conversionType)
    {
        var pageModelType = GetQuickConversionsPageModelType();

        var method = pageModelType.GetMethod($"OnGet{conversionType}");

        Assert.NotNull(method);
    }

    [Theory]
    [InlineData("MilesToKilometers", "10", "16.09")]
    [InlineData("KilometersToMiles", "10", "6.21")]
    [InlineData("FahrenheitToCelsius", "212", "100")]
    [InlineData("CelsiusToFahrenheit", "100", "212")]
    [InlineData("PoundsToKilograms", "10", "4.53")]
    [InlineData("KilogramsToPounds", "10", "22.04")]
    public async Task NamedHandler_PerformsConversion(string handler, string input, string expected)
    {
        await using var application = new WebApplicationFactory<Program>();
        using var client = application.CreateClient();

        var response = await client.GetAsync(
            $"/QuickConversions?handler={handler}&input={input}",
            TestContext.Current.CancellationToken);

        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Contains(expected, content);
    }

    [Fact]
    public async Task QuickConversionsPage_SelectContainsNumericOptions()
    {
        await using var application = new WebApplicationFactory<Program>();
        using var client = application.CreateClient();

        var response = await client.GetAsync("/QuickConversions", TestContext.Current.CancellationToken);

        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Contains("<select", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<option", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task QuickConversionsPage_ContainsLabels()
    {
        await using var application = new WebApplicationFactory<Program>();
        using var client = application.CreateClient();

        var response = await client.GetAsync("/QuickConversions", TestContext.Current.CancellationToken);

        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        var labelCount = content.Split("<label", StringSplitOptions.RemoveEmptyEntries).Length - 1;

        Assert.True(labelCount >= 6, $"Expected at least 6 labels, but found {labelCount}.");
    }

    private static Type GetQuickConversionsPageModelType()
    {
        var candidates = typeof(Program).Assembly
            .GetTypes()
            .Where(type =>
                typeof(PageModel).IsAssignableFrom(type) &&
                !type.IsAbstract &&
                type.GetMethods()
                    .Any(method => method.Name.StartsWith("OnGetMilesToKilometers", StringComparison.Ordinal)))
            .ToList();

        Assert.Single(candidates);

        return candidates[0];
    }
}
