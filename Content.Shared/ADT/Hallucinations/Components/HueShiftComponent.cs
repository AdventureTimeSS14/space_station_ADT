using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Hallucinations.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class HueShiftComponent : Component
{
    [DataField, AutoNetworkedField]
    public float Shift = 0f;
}
