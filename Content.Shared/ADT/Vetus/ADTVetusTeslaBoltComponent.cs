namespace Content.Shared.ADT.Vetus;

[RegisterComponent]
public sealed partial class ADTVetusTeslaBoltComponent : Component
{
    [DataField]
    public int ZapRange = 3;

    [DataField]
    public float Power = 10000f;

    [DataField]
    public float Cutoff = 1000f;

    [DataField]
    public float PowerPerDamage = 600f;

    [DataField]
    public int MaxDamage = 90;

    [DataField]
    public int DamageSpread = 5;

    [DataField]
    public float ForkChance = 0.2f;

    [DataField]
    public float MachineDamagePerPower = 0.0005f;

    [DataField]
    public float MachineEmpChance = 0.4f;

    [DataField]
    public string ArcPrototype = "ADTVetusTeslaArc";
}
