using System;

namespace PowerSimBuilder.Physics;

/// <summary>
/// One physics model inserted into a Python file. Contains bounds for the
/// init block, the update block, or both (one may be missing if manually edited).
/// </summary>
public sealed record PhysicsBlockInfo(
    string ModelName,
    PhysicsBlockBounds? InitBlock,
    PhysicsBlockBounds? UpdateBlock)
{
    public bool HasInit => InitBlock is not null;
    public bool HasUpdate => UpdateBlock is not null;
    public bool HasAny => HasInit || HasUpdate;

    /// <summary>Human-readable summary for the picker UI.</summary>
    public string Summary()
    {
        if (HasInit && HasUpdate)
            return $"init L{InitBlock!.StartLine + 1}, update L{UpdateBlock!.StartLine + 1}";
        if (HasInit)
            return $"init L{InitBlock!.StartLine + 1} (update missing)";
        if (HasUpdate)
            return $"update L{UpdateBlock!.StartLine + 1} (init missing)";
        return "(empty)";
    }
}