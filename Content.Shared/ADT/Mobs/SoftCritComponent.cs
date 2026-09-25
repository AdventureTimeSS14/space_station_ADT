using Content.Shared.Mobs;
using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Mobs;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SoftCritComponent : Component
{
    [DataField, AutoNetworkedField]
    public float ImmobileDamageThreshold = 150f;

    [DataField, AutoNetworkedField]
    public float SpeedModifier = 0.35f;

    public static readonly Dictionary<MobState, (string Sound, bool Loop, float Volume)> StateAudio = new()
    {
        { MobState.SoftCritical, ("/Audio/ADT/Effects/soft_critical.ogg", true, -6f) },
        { MobState.Critical, ("/Audio/ADT/Effects/critical.ogg", true, -8f) },
        { MobState.Alive, ("/Audio/ADT/Effects/backtolife.ogg", false, -4f) },
    };

    public EntityUid? StateAudioEntity;
}
