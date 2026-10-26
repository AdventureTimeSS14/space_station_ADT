using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Xenobiology.Potions;

/// <summary>
/// A potion that makes a creature sentient and available as a ghost role.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SlimeSentiencePotionComponent : Component;
