using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Supermatter.Components;

/// <summary>
/// An entity with this component will not be able to launch supermatter.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SupermatterIgnoreComponent : Component
{

}
