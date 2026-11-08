using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Legion;

[RegisterComponent, NetworkedComponent]
public sealed partial class ADTLegionGateComponent : Component
{
    [DataField]
    public bool Locked;

    [DataField]
    public bool LegionGate;

    [DataField]
    public TimeSpan ConfirmWindow = TimeSpan.FromSeconds(5);

    [DataField]
    public TimeSpan KnockDelay = TimeSpan.FromSeconds(5);

    [DataField]
    public float KnockRange = 1.5f;

    [DataField]
    public SoundSpecifier KnockSound = new SoundPathSpecifier("/Audio/ADT/Legion/shieldbash.ogg");

    [DataField]
    public SoundSpecifier ReleaseSound = new SoundPathSpecifier("/Audio/ADT/Legion/legion_spawn.ogg");

    [DataField]
    public Color ReleaseFlashColor = Color.FromHex("#FF0000");

    [DataField]
    public TimeSpan ReleaseFlashDuration = TimeSpan.FromSeconds(5);

    [ViewVariables]
    public bool Triggered;

    [ViewVariables]
    public Dictionary<EntityUid, TimeSpan> Confirmed = new();

    [ViewVariables]
    public EntityUid? Knocker;

    [ViewVariables]
    public TimeSpan? ReleaseAt;
}
