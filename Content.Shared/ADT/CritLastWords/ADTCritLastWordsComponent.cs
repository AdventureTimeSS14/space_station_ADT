using Content.Shared.FixedPoint;

namespace Content.Shared.ADT.CritLastWords;

/// <summary>
/// Настройки механики последних слов в критическом состоянии: максимальная длина фразы и удушающий урон за каждую.
/// </summary>
[RegisterComponent]
public sealed partial class ADTCritLastWordsComponent : Component
{
    [DataField]
    public int MaxLength = 20;

    [DataField]
    public FixedPoint2 Damage = 20;
}
