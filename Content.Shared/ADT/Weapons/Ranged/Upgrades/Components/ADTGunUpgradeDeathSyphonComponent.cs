using Content.Shared.Damage.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Weapons.Ranged.Upgrades.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class ADTGunUpgradeDeathSyphonComponent : Component
{
    [DataField]
    public float Modifier = 1.25f;

    [DataField]
    public float MegafaunaModifier = 4f;

    [DataField]
    public float MaximumBounty = 25f;

    [DataField]
    public ProtoId<DamageTypePrototype> DamageType = "Blunt";

    [DataField]
    public Dictionary<EntProtoId, float> Bounties = new();
}

[RegisterComponent]
public sealed partial class ADTProjectileDeathSyphonComponent : Component
{
    [DataField]
    public EntityUid? Upgrade;

    [DataField]
    public float Efficiency = 1f;
}

[RegisterComponent]
public sealed partial class ADTSyphonMarkComponent : Component
{
    [DataField]
    public HashSet<EntityUid> Upgrades = new();
}
