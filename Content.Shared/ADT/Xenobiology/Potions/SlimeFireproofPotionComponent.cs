using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Xenobiology.Potions;

/// <summary>
/// A potion that grants full fire and heat protection. Has a limited number of uses.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SlimeFireproofPotionComponent : Component
{
    /// <summary>
    /// How many uses of this potion remain.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public int RemainingUses = 3;
}
