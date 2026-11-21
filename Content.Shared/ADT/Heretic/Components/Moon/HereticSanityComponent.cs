using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Heretic.Components.Moon;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class HereticSanityComponent : Component
{
    [DataField, AutoNetworkedField]
    public float Current = 150f;

    [DataField, AutoNetworkedField]
    public float Max = 150f;

    [DataField]
    public float RegenPerSecond = 1f;

    [DataField]
    public float ConvertThreshold = 25f;
}
