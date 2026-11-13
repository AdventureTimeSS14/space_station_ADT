using Content.Shared.Damage;

namespace Content.Shared.ADT.Body.OrganHealth;

[RegisterComponent]
public sealed partial class CyberOrganComponent : Component
{
    [DataField]
    public float EmpFailureChance = 1f;

    [DataField]
    public TimeSpan DecayDuration = TimeSpan.FromMinutes(5);

    [DataField]
    public TimeSpan EffectCooldown = TimeSpan.FromSeconds(10);

    [DataField]
    public DamageSpecifier? EmpDamage;

    [DataField]
    public TimeSpan EmpJitter;

    [DataField]
    public bool EmpVomit;

    [DataField]
    public float EmpOrganDamageChance;

    [DataField]
    public float EmpOrganDamage;

    [ViewVariables]
    public bool Decaying;

    [ViewVariables]
    public TimeSpan NextEffect;

    [ViewVariables]
    public TimeSpan NextDecay;
}
