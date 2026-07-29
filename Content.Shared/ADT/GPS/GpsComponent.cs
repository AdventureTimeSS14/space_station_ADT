using Robust.Shared.GameStates;

namespace Content.Shared.ADT.GPS;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class GpsComponent : Component
{
    [DataField, AutoNetworkedField]
    public string Tag = "MINE0";

    [DataField]
    public int MaxTagLength = 5;

    [DataField, AutoNetworkedField]
    public bool Tracking;

    [DataField]
    public bool CanToggle = true;

    [DataField, AutoNetworkedField]
    public bool SameMapOnly;

    [DataField]
    public float UpdateRate = 1f;

    [ViewVariables]
    public TimeSpan NextUpdate;
}
