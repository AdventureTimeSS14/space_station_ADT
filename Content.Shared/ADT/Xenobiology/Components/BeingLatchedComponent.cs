using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Xenobiology.Components;

/// <summary>
/// Marks an entity as being consumed so it is not targeted by other entities.
/// Freaky.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class BeingLatchedComponent : Component;
