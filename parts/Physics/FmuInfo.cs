#nullable enable
using System.Collections.Generic;

namespace PowerSimBuilder.Physics;

/// <summary>
/// Parsed information from an FMU's modelDescription.xml.
/// </summary>
public sealed record FmuInfo(
    string ModelName,
    string FmiVersion,
    string FmiType,
    IReadOnlyList<string> Inputs,
    IReadOnlyList<string> Outputs,
    IReadOnlyList<FmuParameter> Parameters,
    FmuTiming? Timing,
    IReadOnlyDictionary<string, string>? InputTypes = null,
    IReadOnlyDictionary<string, string>? OutputTypes = null);

public sealed record FmuParameter(string Name, string StartValue, string Type = "Real");

public sealed record FmuTiming(double? StartTime, double? StopTime, double? StepSize);