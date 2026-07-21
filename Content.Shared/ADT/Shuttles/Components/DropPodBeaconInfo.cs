using System.Numerics;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Shuttles.Components;

/// <summary>
/// Identifies a valid (non-blacklisted) landing beacon sent to the client UI.
/// </summary>
[Serializable, NetSerializable]
public sealed class DropPodBeaconInfo
{
    public NetEntity Uid { get; init; }
    public string Name { get; init; } = string.Empty;
    /// <summary>World-space position used to highlight this beacon on the nav map.</summary>
    public Vector2 WorldPos { get; init; }
}
