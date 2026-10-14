using Content.Shared.Tools;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Janicart.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedADTJanicartSystem))]
public sealed partial class ADTJanicartUpgradeableComponent : Component
{
    [DataField]
    public string UpgradesContainerId = "janicart_upgrades";

    [DataField]
    public float RemoveDelay = 1f;

    [DataField]
    public ProtoId<ToolQualityPrototype> RemoveTool = "Prying";

    [DataField]
    public SoundSpecifier InsertSound = new SoundPathSpecifier("/Audio/Items/screwdriver2.ogg");

    [DataField]
    public SoundSpecifier RemoveSound = new SoundPathSpecifier("/Audio/Items/crowbar.ogg");
}

[Serializable, NetSerializable]
public enum ADTJanicartUpgradeVisuals : byte
{
    Buffer,
}