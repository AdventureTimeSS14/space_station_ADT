using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Areas;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AreaGridComponent : Component
{
    [DataField, AutoNetworkedField]
    public Dictionary<Vector2i, EntProtoId<AreaComponent>> Areas = new();
}
