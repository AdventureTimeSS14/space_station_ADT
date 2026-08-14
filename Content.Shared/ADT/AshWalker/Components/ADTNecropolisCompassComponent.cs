using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.AshWalker.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ADTNecropolisCompassComponent : Component
{
    [DataField]
    public TimeSpan Delay = TimeSpan.FromSeconds(2);

    [DataField]
    public EntProtoId ActionId = "ADTActionNecropolisCompass";

    [DataField, AutoNetworkedField]
    public EntityUid? ActionEntity;
}
