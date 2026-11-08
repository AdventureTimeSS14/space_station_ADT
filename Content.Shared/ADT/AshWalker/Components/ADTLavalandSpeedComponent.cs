using Robust.Shared.GameStates;

namespace Content.Shared.ADT.AshWalker.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ADTLavalandSpeedComponent : Component
{
    [DataField, AutoNetworkedField]
    public float WalkModifier = 1.15f;

    [DataField, AutoNetworkedField]
    public float SprintModifier = 1.15f;

    [DataField, AutoNetworkedField]
    public bool Everywhere;
}
