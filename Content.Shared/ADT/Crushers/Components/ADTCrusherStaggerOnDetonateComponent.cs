using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Crushers.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ADTCrusherStaggerOnDetonateComponent : Component
{
    [DataField, AutoNetworkedField]
    public int Hits = 3;

    [DataField, AutoNetworkedField]
    public int MegafaunaHits = 6;

    [DataField, AutoNetworkedField]
    public TimeSpan StunDuration = TimeSpan.FromSeconds(1.5);

    [DataField, AutoNetworkedField]
    public TimeSpan MegafaunaVulnerableDuration = TimeSpan.FromSeconds(2);

    [DataField, AutoNetworkedField]
    public TimeSpan StackDecay = TimeSpan.FromSeconds(6);

    [DataField]
    public EntProtoId VulnerableEffect = "ADTStatusEffectCrusherVulnerable";

    [DataField]
    public SoundSpecifier? StaggerSound;
}
