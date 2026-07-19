using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Light;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class LightVisibilityComponent : Component
{
    [DataField, AutoNetworkedField]
    public float MinLight = 0f;

    [DataField, AutoNetworkedField]
    public float MaxLight = 0.005f;

    [DataField, AutoNetworkedField]
    public float EdgeSoftness = 0.007f;

    [DataField, AutoNetworkedField]
    public bool ShowInside = true;

    [DataField, AutoNetworkedField]
    public bool BlockExamine = true;

    public bool HadOutline = false;
}
