using Content.Shared.Nutrition.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Cooking.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class ADTWellFedStatusEffectComponent : Component
{
    [DataField]
    public Dictionary<ProtoId<SatiationTypePrototype>, float> RateMultipliers = new();
}
