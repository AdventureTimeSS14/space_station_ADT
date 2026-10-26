using Content.Shared.Tag;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Xenobiology.XenobiologyControlConsole;

[RegisterComponent]
public sealed partial class XenobiologyControlConsoleComponent : Component
{
    [DataField]
    public int MaxSlimeCapacity = 5;

    [DataField]
    public float InteractRange = 0.1f;

    [DataField]
    public ProtoId<TagPrototype> MonkeyCubeTag = "MonkeyCube";

    [DataField]
    public SoundSpecifier? SuctionSound = new SoundPathSpecifier("/Audio/Effects/zzzt.ogg");

    [DataField]
    public SoundSpecifier? EjectSound = new SoundPathSpecifier("/Audio/Effects/trashbag3.ogg");

    [DataField]
    public int MutationPotions;

    [DataField]
    public int StabilizerPotions;

    public const string SlimeContainerId = "xenobio_control_slimes";
}
