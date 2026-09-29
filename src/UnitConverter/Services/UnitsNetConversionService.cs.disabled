using UnitConverter.Models;
using UnitsNet;

namespace UnitConverter.Services;

/// <summary>
/// Alternate conversion service used for the Lesson 5 dependency-injection experiment.
/// This file is intentionally disabled until later in the assignment.
/// </summary>
public class UnitsNetConversionService : IConversionService
{
    public decimal Convert(decimal value, string conversionType)
    {
        var convertedValue = conversionType switch
        {
            ConversionTypes.MilesToKilometers => Length.FromMiles((double)value).Kilometers,
            ConversionTypes.KilometersToMiles => Length.FromKilometers((double)value).Miles,
            ConversionTypes.FahrenheitToCelsius => Temperature.FromDegreesFahrenheit((double)value).DegreesCelsius,
            ConversionTypes.CelsiusToFahrenheit => Temperature.FromDegreesCelsius((double)value).DegreesFahrenheit,
            ConversionTypes.PoundsToKilograms => Mass.FromPounds((double)value).Kilograms,
            ConversionTypes.KilogramsToPounds => Mass.FromKilograms((double)value).Pounds,
            _ => throw new ArgumentException(
                $"Unsupported conversion type: {conversionType}",
                nameof(conversionType))
        };

        // Preserve the two-decimal behavior established by the existing application.
        return decimal.Truncate((decimal)convertedValue * 100m) / 100m;
    }
}
