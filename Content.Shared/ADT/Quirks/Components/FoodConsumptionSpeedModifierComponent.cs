using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Traits;

[RegisterComponent]
[NetworkedComponent]
public sealed partial class FoodConsumptionSpeedModifierComponent : Component
{
    [DataField]
    public float Modifier = 0.5f;
}
