using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Medical.Patch;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ADTPatchComponent : Component
{
    [DataField]
    public string Solution = "pen";

    [DataField]
    public FixedPoint2 TransferRate = FixedPoint2.New(0.35);

    [DataField]
    public TimeSpan TransferDelay = TimeSpan.FromSeconds(1);

    [DataField]
    public float StackMultiplier = 1.5f;

    [DataField]
    public TimeSpan Delay = TimeSpan.FromSeconds(3);

    [DataField]
    public SoundSpecifier? ApplySound = new SoundPathSpecifier("/Audio/Items/Medical/brutepack_end.ogg");

    [DataField, AutoNetworkedField]
    public EntityUid? AppliedTo;

    [DataField, AutoNetworkedField]
    public TimeSpan NextTransfer;
}
