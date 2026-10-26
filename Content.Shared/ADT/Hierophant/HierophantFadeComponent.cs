using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Hierophant;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class HierophantFadeComponent : Component
{
    [DataField, AutoNetworkedField]
    public float StartAlpha = 1f;

    [DataField, AutoNetworkedField]
    public float EndAlpha;

    [DataField, AutoNetworkedField]
    public TimeSpan StartTime;

    [DataField, AutoNetworkedField]
    public TimeSpan Duration = TimeSpan.FromSeconds(0.2);
}
