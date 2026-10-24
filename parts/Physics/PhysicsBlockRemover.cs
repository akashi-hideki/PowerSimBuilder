using System;

namespace PowerSimBuilder.Physics;

/// <summary>Result of a physics block removal.</summary>
public sealed record PhysicsBlockRemovalResult(
    bool Success,
    string? NewCode,
    string? ErrorMessage,
    int RemovedInitCount,
    int RemovedUpdateCount);

/// <summary>
/// Removes a physics model's init and/or update blocks from a Python source.
/// Helper definitions (from fmpy import ..., _psb_resolve_fmu) are NOT removed;
/// this is intentionally left to a future cleanup pass (MVP-1.3b) so that
/// removal cannot accidentally break other code that still uses them.
/// </summary>
public static class PhysicsBlockRemover
{
    public static PhysicsBlockRemovalResult Remove(string? code, string modelName)
    {
        if (string.IsNullOrEmpty(code))
            return Fail("Current code is null or empty.");
        if (string.IsNullOrEmpty(modelName))
            return Fail("Model name is required.");

        var init = PhysicsInsertionHighlighter.FindBlock(code, modelName);
        var update = PhysicsInsertionHighlighter.FindUpdateBlock(code, modelName);

        if (init is null && update is null)
            return Fail($"No physics block found for '{modelName}'.");

        // Build removal ranges; remove later positions first so earlier offsets stay valid.
        var ranges = new System.Collections.Generic.List<(int Start, int Length)>();
        if (init is not null) ranges.Add((init.Start, init.Length));
        if (update is not null) ranges.Add((update.Start, update.Length));
        ranges.Sort((a, b) => b.Start.CompareTo(a.Start));

        var newCode = code;
        int removedInit = 0, removedUpdate = 0;

        foreach (var (start, length) in ranges)
        {
            if (start < 0 || start + length > newCode.Length)
                continue;

            int actual = length;
            // Include trailing CRLF / LF so we don't leave blank lines
            if (start + actual + 1 < newCode.Length &&
                newCode[start + actual] == '\r' && newCode[start + actual + 1] == '\n')
                actual += 2;
            else if (start + actual < newCode.Length && newCode[start + actual] == '\n')
                actual += 1;

            newCode = newCode.Remove(start, actual);

            // Determine which block this was by original range
            if (init is not null && start == init.Start) removedInit++;
            else if (update is not null && start == update.Start) removedUpdate++;
        }

        return new PhysicsBlockRemovalResult(
            true, newCode, null, removedInit, removedUpdate);
    }

    private static PhysicsBlockRemovalResult Fail(string msg)
        => new(false, null, msg, 0, 0);
}