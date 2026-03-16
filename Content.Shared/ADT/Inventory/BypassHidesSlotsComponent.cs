using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Inventory;

/// <summary>
/// Component that allows the entity to ignore HidesSlots restrictions.
/// Used by ghosts and admin ghosts to see hidden clothing slots.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class BypassHidesSlotsComponent : Component;
