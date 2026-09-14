using System.Numerics;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Shuttles.Components;

/// <summary>
/// Sent by the client to request launching toward a specific beacon.
/// The server applies a random offset so the exact tile is never revealed in advance.
/// </summary>
[Serializable, NetSerializable]
public sealed class DropPodConsoleDeployMessage : BoundUserInterfaceMessage
{
    public NetEntity TargetBeacon { get; init; }
}
