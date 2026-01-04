using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Shizophrenia;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class HueShiftComponent : Component
{
    [DataField, AutoNetworkedField]
    public float Shift = 0f;
}
