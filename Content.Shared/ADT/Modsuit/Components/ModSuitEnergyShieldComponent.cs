using Robust.Shared.GameStates;

namespace Content.Shared.ADT.ModSuits;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ModSuitEnergyShieldComponent : Component
{
    [DataField]
    public float DamagePerBlock = 1f;

    [ViewVariables, AutoNetworkedField]
    public float MaxHealth;

    [ViewVariables, AutoNetworkedField]
    public float CurrentHealth;

    [ViewVariables]
    public EntityUid? Bubble;

    [ViewVariables]
    public EntityUid? Wearer;
}
