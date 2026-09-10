using Robust.Shared.Audio;

namespace Content.Shared.ADT.Xenobiology.XenobiologyControlConsole;

[RegisterComponent]
public sealed partial class XenobiologyControlConsoleComponent : Component
{
    [DataField]
    public int MaxSlimeCapacity = 5;

    [DataField]
    public float InteractRange = 0.1f;

    [DataField]
    public SoundSpecifier? SuctionSound = new SoundPathSpecifier("/Audio/Effects/zzzt.ogg");

    [DataField]
    public SoundSpecifier? EjectSound = new SoundPathSpecifier("/Audio/Effects/trashbag3.ogg");

    public const string SlimeContainerId = "xenobio_control_slimes";
}
