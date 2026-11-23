using Content.Shared.ADT.Cooking.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Cooking.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class ADTDishComponent : Component
{
    [DataField, AutoNetworkedField]
    public ADTCookingMethod Method = ADTCookingMethod.Raw;

    [DataField, AutoNetworkedField]
    public ProtoId<ADTDishQualityPrototype> Quality = "ADTDishQualityNormal";

    [DataField, AutoNetworkedField]
    public float Burn;

    [DataField, AutoNetworkedField]
    public Color? Glaze;

    [DataField, AutoNetworkedField]
    public Color? Spice;

    [DataField, AutoNetworkedField]
    public List<EntProtoId> Toppings = new();
}
