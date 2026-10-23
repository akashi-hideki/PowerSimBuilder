#nullable enable
using System;
using System.IO;

namespace PowerSimBuilder.Physics;

public sealed record PhysicsInsertionResult(
    InsertOutcome Outcome,
    string? NewCode,
    string? ErrorMessage)
{
    public bool Succeeded => ErrorMessage == null && Outcome == InsertOutcome.Inserted;
}

public static class PhysicsInsertionFlow
{
    public static bool IsPhysicsTarget(string? componentType)
        => !string.Equals(componentType, "ControllerBox", StringComparison.Ordinal);

    public static PhysicsInsertionResult TryInsert(
        string? currentCode,
        PhysicsModelEntry? entry)
    {
        if (currentCode == null) return Fail("Current Python code is null.");
        if (entry == null) return Fail("No physics model was selected.");

        if (!File.Exists(entry.FmuPath))
            return Fail("FMU file not found: " + entry.FmuPath);

        if (!string.Equals(entry.Info.FmiType, "CoSimulation", StringComparison.Ordinal))
            return Fail("Only CoSimulation FMUs are supported (got "
                + entry.Info.FmiType + ").");

        try
        {
            var modelYamlPath = Path.Combine(entry.ModelDirectory, "model.yaml");
            var mapping = ModelYaml.ReadPortMapping(modelYamlPath);

            var info = entry.Info;
            var helper = PhysicsSnippetBuilder.BuildHelper();
            var initBlock = PhysicsSnippetBuilder.BuildInsertionBlock(
                info, entry.FmuRelativePath, mapping: mapping);
            var updateBlock = PhysicsSnippetBuilder.BuildUpdateBlock(
                info, mapping: mapping);

            var (outcome, code) = PhysicsSnippetInserter.Insert(
                currentCode, info.ModelName, initBlock, helper, updateBlock);

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