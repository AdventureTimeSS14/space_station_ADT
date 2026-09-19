using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Crushers.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class KineticCooldownTrophyEffectComponent : Component
{
    /// <summary>
    /// How much faster the weapon swings while this trophy is installed. 1 leaves melee alone.
    /// </summary>
    [DataField]
    public float MeleeCoefficient = 1f;

    /// <summary>
    /// How much faster the weapon shoots while this trophy is installed. 1 leaves shooting alone.
    /// </summary>
    [DataField]
    public float RangedCoefficient = 1f;

    [DataField, AutoNetworkedField]
    public bool Applied;
}
