using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.AshWalker.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ADTIgniteComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntProtoId Ember = "ADTAshWalkerEmber";

    [DataField]
    public EntProtoId ActionId = "ADTActionIgnite";

    [DataField, AutoNetworkedField]
    public EntityUid? ActionEntity;
}
