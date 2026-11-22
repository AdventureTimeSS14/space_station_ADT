namespace Content.Shared.ADT.Virology.Effects;

public sealed partial class ADTSymptomAggression : ADTSymptomEffect<ADTSymptomAggression>
{
    [DataField]
    public float Range = 1.5f;

    [DataField]
    public float AttackChance = 0.5f;
}
