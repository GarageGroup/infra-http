using System;
using System.Globalization;

namespace GarageGroup.Infra;

public static partial class HttpConfigurationExtensions
{
    private static string? GetVariableFromSection(string? sectionName, string variableName)
        =>
        string.IsNullOrEmpty(sectionName)
        ? Environment.GetEnvironmentVariable(variableName)
        : Environment.GetEnvironmentVariable(sectionName + ":" + variableName);

    private static TimeSpan? ParseTimeSpan(this string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return TimeSpan.TryParse(value, CultureInfo.InvariantCulture, out var parsedTimeSpan)
            ? parsedTimeSpan
            : throw new FormatException($"Invalid TimeSpan value '{value}'.");
    }
}
