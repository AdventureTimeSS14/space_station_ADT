using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.BossMusic;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ADTBossMusicListenerComponent : Component
{
    [DataField, AutoNetworkedField]
    public ProtoId<ADTBossMusicPrototype> Music;

    [DataField, AutoNetworkedField]
    public EntityUid? Boss;
}
