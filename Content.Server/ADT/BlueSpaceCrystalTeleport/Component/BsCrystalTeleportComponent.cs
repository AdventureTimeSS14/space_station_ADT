using Robust.Shared.Audio;

namespace Content.Server.ADT.BlueSpaceCrystalTeleport;

/// <summary>
/// Component, that make possible to teleport owner of entity on use at random cords in radius, target on throw and can be used like projectile and if in stack more 1 entity , count in stack adding to radius
/// </summary>
[RegisterComponent]
public sealed partial class BsCrystalTeleportComponent : Component
{
    /// <summary>
    /// Radius of teleport on use in hand
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float TeleportRadius = 4f;

    /// <summary>
    /// Teleport radius on throw . If in stack of entity(not owner entity) more than 1 entity, count adding to radius
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float TeleportRadiusThrow = 2f;

    /// <summary>
    /// Sound of teleport
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier TeleportSound = new SoundPathSpecifier("/Audio/Effects/teleport_departure.ogg");
}
