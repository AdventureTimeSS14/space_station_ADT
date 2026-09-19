using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Weapons.Ranged.Upgrades.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class ADTGunUpgradeAoEComponent : Component
{
    [DataField]
    public bool MineTiles;

    [DataField]
    public float MobDamageModifier;

    [DataField]
    public float Radius = 1.5f;

    [DataField]
    public EntProtoId? Effect = "ADTPKAExplosionEffect";
}

[RegisterComponent]
public sealed partial class ADTProjectileAoEComponent : Component
{
    [DataField]
    public bool MineTiles;

    [DataField]
    public float MobDamageModifier;

    [DataField]
    public float Radius = 1.5f;

    [DataField]
    public EntProtoId? Effect;

    [ViewVariables]
    public ADTVolleyTrigger? Volley;
}
