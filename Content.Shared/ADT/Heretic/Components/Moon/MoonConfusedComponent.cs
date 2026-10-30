using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Heretic.Components.Moon;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MoonConfusedComponent : Component
{
    [DataField, AutoNetworkedField]
    public float ShakeInterval = 0.2f;

    [DataField, AutoNetworkedField]
    public float ShakeStrength = 0.6f;
}