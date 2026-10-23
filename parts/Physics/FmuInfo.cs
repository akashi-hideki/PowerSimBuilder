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
    FmuTiming? Timing);

public sealed record FmuParameter(string Name, string StartValue);

public sealed record FmuTiming(double? StartTime, double? StopTime, double? StepSize);