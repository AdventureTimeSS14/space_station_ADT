using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Blob.Components;

/// <summary>
/// Marker component preventing teleportation onto/from this entity.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class BlockTeleportComponent : Component
{
}
