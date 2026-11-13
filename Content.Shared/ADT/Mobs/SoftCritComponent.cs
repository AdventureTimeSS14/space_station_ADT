using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Mobs;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SoftCritComponent : Component
{
    [DataField, AutoNetworkedField]
    public float SpeedModifier = 0.35f;

    [DataField]
    public SoundSpecifier? SoftCritSound = new SoundPathSpecifier("/Audio/ADT/Effects/soft_critical.ogg", AudioParams.Default.WithLoop(true).WithVolume(-6f));

    [DataField]
    public SoundSpecifier? CritSound = new SoundPathSpecifier("/Audio/ADT/Effects/critical.ogg", AudioParams.Default.WithLoop(true).WithVolume(-8f));

    [DataField]
    public SoundSpecifier? ReviveSound = new SoundPathSpecifier("/Audio/ADT/Effects/backtolife.ogg", AudioParams.Default.WithVolume(-4f));

    [ViewVariables]
    public EntityUid? StateAudio;
}
