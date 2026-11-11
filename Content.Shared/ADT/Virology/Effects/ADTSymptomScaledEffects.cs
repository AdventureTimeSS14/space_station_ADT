using Content.Shared.Damage;

namespace Content.Shared.ADT.Virology.Effects;

public enum ADTSymptomStat : byte
{
    StageSpeed,
    Stealth,
    Resistance,
    Transmittable,
}

[DataDefinition]
public sealed partial class ADTSymptomTerm
{
    [DataField]
    public float Base;

    [DataField]
    public ADTSymptomStat? Stat;

    [DataField]
    public float Factor = 1f;

    [DataField]
    public bool Sqrt = true;

    [DataField]
    public float Multiplier = 1f;
}

public abstract partial class ADTSymptomScaledEffect<T> : ADTSymptomEffect<T> where T : ADTSymptomScaledEffect<T>
{
    [DataField(required: true)]
    public List<ADTSymptomTerm> Terms = new();

    [DataField]
    public bool ScaleByStage;

    [DataField]
    public bool Randomize;

    [DataField]
    public float Multiplier = 1f;

    [DataField]
    public float Minimum = float.MinValue;
}

public sealed partial class ADTSymptomScaledDamage : ADTSymptomScaledEffect<ADTSymptomScaledDamage>
{
    [DataField(required: true)]
    public DamageSpecifier Damage = default!;
}

public sealed partial class ADTSymptomScaledTemperature : ADTSymptomScaledEffect<ADTSymptomScaledTemperature>
{
    [DataField]
    public bool Heat = true;

    [DataField]
    public float? MaxTemperature;

    [DataField]
    public float? MinTemperature;
}

public sealed partial class ADTSymptomScaledFire : ADTSymptomScaledEffect<ADTSymptomScaledFire>
{
    [DataField]
    public bool Ignite = true;

    [DataField]
    public float DamageRatio = 0.5f;
}

public sealed partial class ADTSymptomSpread : ADTSymptomEffect<ADTSymptomSpread>
{
    [DataField]
    public int Range = 5;
}

public sealed partial class ADTSymptomDropItem : ADTSymptomEffect<ADTSymptomDropItem>
{
}

public sealed partial class ADTSymptomDisorient : ADTSymptomEffect<ADTSymptomDisorient>
{
    [DataField]
    public float Time = 12f;
}

public sealed partial class ADTSymptomStamina : ADTSymptomEffect<ADTSymptomStamina>
{
    [DataField]
    public float Amount = 15f;
}
