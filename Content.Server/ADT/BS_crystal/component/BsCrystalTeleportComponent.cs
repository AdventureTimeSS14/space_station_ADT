using Content.Shared.DoAfter;
using Content.Shared.Actions;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Audio;
using Robust.Server.GameObjects;
using Content.Shared.Interaction.Events;
using Content.Server.ADT.Bs_crystal;


namespace Content.Server.ADT.Bs_crystal;
[RegisterComponent]
public sealed partial class BsCrystalTeleportComponent : Component
{

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float TeleportRadius = 40f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float TeleportRadiusThrow = 20f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier TeleportSound = new SoundPathSpecifier("/Audio/Effects/teleport_departure.ogg");



}
