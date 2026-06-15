using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Crushers.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MeleeChargeActiveEffectComponent : Component
{
    [DataField, AutoNetworkedField]
    public DamageSpecifier BonusDamage = new();
}
