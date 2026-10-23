#nullable enable
using System;
using System.IO;

namespace PowerSimBuilder.Physics;

/// <summary>
/// Result of a single insertion attempt.
/// </summary>
public sealed record PhysicsInsertionResult(
    InsertOutcome Outcome,
    string? NewCode,
    string? ErrorMessage)
{
    public bool Succeeded => ErrorMessage == null && Outcome == InsertOutcome.Inserted;
}

/// <summary>
/// Orchestrates insertion with validation and error handling,
/// kept free of UI dependencies so it can be unit-tested.
/// </summary>
public static class PhysicsInsertionFlow
{
    /// <summary>
    /// Physics models must not be attached to StaTable-imported components
    /// (Type == "ControllerBox"). Returns true for all other types.
    /// </summary>
    public static bool IsPhysicsTarget(string? componentType)
        => !string.Equals(componentType, "ControllerBox", StringComparison.Ordinal);

    public static PhysicsInsertionResult TryInsert(
        string? currentCode,
        PhysicsModelEntry? entry)
    {
        if (currentCode == null)
            return Fail("Current Python code is null.");
        if (entry == null)
            return Fail("No physics model was selected.");

        if (!IsPhysicsTarget(entry.Info.FmiType) && false)
        {
            // placeholder: FMI type filter applied elsewhere
        }

        if (!File.Exists(entry.FmuPath))
            return Fail("FMU file not found: " + entry.FmuPath);

        if (!string.Equals(entry.Info.FmiType, "CoSimulation", StringComparison.Ordinal))
            return Fail("Only CoSimulation FMUs are supported (got "
                + entry.Info.FmiType + ").");

        try
        {
            var (outcome, code) = PhysicsSnippetInserter.InsertFromFmu(
                currentCode, entry.FmuPath, entry.FmuRelativePath);

            return new PhysicsInsertionResult(outcome, code, null);
        }
        catch (Exception ex)
        {
            return Fail(ex.Message);
        }
    }

    private static PhysicsInsertionResult Fail(string message)
        => new PhysicsInsertionResult(InsertOutcome.NoInitFound, null, message);
}