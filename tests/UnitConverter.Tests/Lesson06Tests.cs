using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace UnitConverter.Tests;

public class Lesson06Tests
{
    [Fact]
    public void LogEntry_Exists()
    {
        Assert.NotNull(GetTypeByName("LogEntry"));
    }

    [Fact]
    public void ILogReader_ExistsAndIsAnInterface()
    {
        var type = GetTypeByName("ILogReader");
        Assert.True(type.IsInterface, "ILogReader should be declared as an interface.");
    }

    [Fact]
    public void JsonLogReader_ImplementsILogReader()
    {
        var interfaceType = GetTypeByName("ILogReader");
        var implementationType = GetTypeByName("JsonLogReader");

        Assert.True(
            interfaceType.IsAssignableFrom(implementationType),
            "JsonLogReader should implement ILogReader.");
    }

    [Fact]
    public void DependencyInjection_ResolvesILogReader()
    {
        using var application = new WebApplicationFactory<Program>();
        var interfaceType = GetTypeByName("ILogReader");

        var service = application.Services.GetService(interfaceType);

        Assert.NotNull(service);
        Assert.True(interfaceType.IsInstanceOfType(service));
    }

    [Fact]
    public void JsonLogReader_ReadsMultipleFilesAndSkipsMalformedLines()
    {
        using var application = new WebApplicationFactory<Program>();
        var interfaceType = GetTypeByName("ILogReader");
        var environment = application.Services.GetRequiredService<IWebHostEnvironment>();
        var logDirectory = Path.Combine(environment.ContentRootPath, "Logs");

        Directory.CreateDirectory(logDirectory);

        var id = Guid.NewGuid().ToString("N");
        var firstFile = Path.Combine(logDirectory, $"unitconverter-test-{id}-a.json");
        var secondFile = Path.Combine(logDirectory, $"unitconverter-test-{id}-b.json");

        try
        {
            File.WriteAllLines(firstFile,
            [
                """{"@t":"2026-10-06T14:00:00Z","@m":"Converted 10 using MilesToKilometers with result 16.09","Input":10,"ConversionType":"MilesToKilometers","Result":16.09}""",
                """{"@t":"2026-10-06T14:01:00Z","@m":"Invalid conversion input","@l":"Warning","Input":"not-a-number","ConversionType":"PoundsToKilograms"}"""
            ]);

            File.WriteAllLines(secondFile,
            [
                """this is deliberately malformed JSON""",
                """{"@t":"2026-10-06T14:02:00Z","@m":"Conversion failed","@l":"Error","@x":"System.InvalidOperationException: Controlled test failure","ConversionType":"CelsiusToFahrenheit"}"""
            ]);

            var reader = application.Services.GetRequiredService(interfaceType);
            var entries = InvokeRead(reader).ToList();

            Assert.Contains(entries, entry =>
                GetStringProperty(entry, "ConversionType") == "MilesToKilometers" &&
                GetStringProperty(entry, "Level") == "Information");

            Assert.Contains(entries, entry =>
                GetStringProperty(entry, "Level") == "Warning");

            Assert.Contains(entries, entry =>
                GetStringProperty(entry, "Level") == "Error" &&
                (GetStringProperty(entry, "Exception") ?? "")
                    .Contains("Controlled test failure", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            DeleteIfExists(firstFile);
            DeleteIfExists(secondFile);
        }
    }

    [Fact]
    public async Task LogsPage_IsAvailable()
    {
        await using var application = new WebApplicationFactory<Program>();
        using var client = application.CreateClient();

        var response = await client.GetAsync(
            "/Logs",
            TestContext.Current.CancellationToken);

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task LogsPage_HasAccessibleSearchControlsAndTable()
    {
        await using var application = new WebApplicationFactory<Program>();
        using var client = application.CreateClient();

        var response = await client.GetAsync(
            "/Logs",
            TestContext.Current.CancellationToken);

        var content = await response.Content.ReadAsStringAsync(
            TestContext.Current.CancellationToken);

        Assert.Contains("<form", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<label", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<table", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<th", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LogsPageModel_ReceivesILogReader()
    {
        var interfaceType = GetTypeByName("ILogReader");
        var pageModelType = GetLogsPageModelType();

        var found = pageModelType
            .GetConstructors()
            .Any(constructor => constructor.GetParameters()
                .Any(parameter => parameter.ParameterType == interfaceType));

        Assert.True(
            found,
            "The Logs PageModel should receive ILogReader through constructor injection.");
    }

    [Fact]
    public void LogsPageModel_HasGetFilterProperties()
    {
        var pageModelType = GetLogsPageModelType();

        Assert.NotNull(pageModelType.GetProperty("Search"));
        Assert.NotNull(pageModelType.GetProperty("Level"));
        Assert.NotNull(pageModelType.GetProperty("ConversionType"));
    }

    [Theory]
    [InlineData("level=Warning", "Invalid conversion input")]
    [InlineData("conversionType=MilesToKilometers", "16.09")]
    [InlineData("search=Controlled", "Controlled test failure")]
    public async Task LogsPage_FiltersEntries(string query, string expected)
    {
        await using var application = new WebApplicationFactory<Program>();
        var environment = application.Services.GetRequiredService<IWebHostEnvironment>();
        var logDirectory = Path.Combine(environment.ContentRootPath, "Logs");
        Directory.CreateDirectory(logDirectory);

        var file = Path.Combine(
            logDirectory,
            $"unitconverter-test-{Guid.NewGuid():N}.json");

        try
        {
            File.WriteAllLines(file,
            [
                """{"@t":"2026-10-06T14:00:00Z","@m":"Converted 10 using MilesToKilometers with result 16.09","Input":10,"ConversionType":"MilesToKilometers","Result":16.09}""",
                """{"@t":"2026-10-06T14:01:00Z","@m":"Invalid conversion input","@l":"Warning","Input":"not-a-number","ConversionType":"PoundsToKilograms"}""",
                """{"@t":"2026-10-06T14:02:00Z","@m":"Conversion failed: Controlled test failure","@l":"Error","@x":"System.InvalidOperationException: Controlled test failure","ConversionType":"CelsiusToFahrenheit"}"""
            ]);

            using var client = application.CreateClient();

            var response = await client.GetAsync(
                $"/Logs?{query}",
                TestContext.Current.CancellationToken);

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(
                TestContext.Current.CancellationToken);

            Assert.Contains(expected, content, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            DeleteIfExists(file);
        }
    }

    private static IEnumerable<object> InvokeRead(object reader)
    {
        var readMethod = reader.GetType().GetMethod("Read", BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(readMethod);

        var result = readMethod.Invoke(reader, null);
        Assert.NotNull(result);

        return Assert.IsAssignableFrom<System.Collections.IEnumerable>(result)
            .Cast<object>();
    }

    private static string? GetStringProperty(object instance, string propertyName)
    {
        var property = instance.GetType().GetProperty(propertyName);
        Assert.NotNull(property);

        return property.GetValue(instance)?.ToString();
    }

    private static void DeleteIfExists(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    private static Type GetTypeByName(string name)
    {
        var candidates = typeof(Program).Assembly
            .GetTypes()
            .Where(type => type.Name == name)
            .ToList();

        Assert.True(
            candidates.Count == 1,
            $"Expected exactly one type named {name}, but found {candidates.Count}.");

        return candidates[0];
    }

    private static Type GetLogsPageModelType()
    {
        var candidates = typeof(Program).Assembly
            .GetTypes()
            .Where(type =>
                typeof(PageModel).IsAssignableFrom(type) &&
                !type.IsAbstract &&
                type.Name.Contains("Logs", StringComparison.OrdinalIgnoreCase))
            .ToList();

        Assert.Single(candidates);
        return candidates[0];
    }
}
