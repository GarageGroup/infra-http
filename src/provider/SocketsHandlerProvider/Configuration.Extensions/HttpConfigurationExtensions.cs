using System;

namespace GarageGroup.Infra;

public static partial class HttpConfigurationExtensions
{
    private static string? GetVariableFromSection(string? sectionName, string variableName)
        =>
        string.IsNullOrEmpty(sectionName)
        ? Environment.GetEnvironmentVariable(variableName)
        : Environment.GetEnvironmentVariable(sectionName + ":" + variableName);

    private static TimeSpan? ParseTimeSpan(this string? value)
        =>
        string.IsNullOrEmpty(value) ? null : TimeSpan.Parse(value);
}