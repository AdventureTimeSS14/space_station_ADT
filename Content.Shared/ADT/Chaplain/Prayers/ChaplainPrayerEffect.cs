using Content.Shared.ADT.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Chaplain.Prayers;

[ImplicitDataDefinitionForInheritors]
public abstract partial class ChaplainPrayerEffect;

public sealed partial class HealPrayerEffect : ChaplainPrayerEffect
{
    [DataField(required: true)]
    public GroupHealSpecifier Heal = new();
}

public sealed partial class FaithPrayerEffect : ChaplainPrayerEffect
{
    [DataField]
    public FixedPoint2 Amount = 1;
}

public sealed partial class RepelPrayerEffect : ChaplainPrayerEffect
{
    [DataField]
    public float Range = 3f;

    [DataField]
    public float ThrowDistance = 2f;

    [DataField]
    public FixedPoint2 Suppression = 15;

    [DataField]
    public TimeSpan Slowdown = TimeSpan.FromSeconds(2);

    [DataField]
    public float SlowdownMultiplier = 0.6f;
}

public sealed partial class ShieldPrayerEffect : ChaplainPrayerEffect
{
    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(15);
}

public sealed partial class RallyPrayerEffect : ChaplainPrayerEffect
{
    [DataField]
    public float Range = 5f;

    [DataField]
    public EntProtoId SpeedEffect = "ADTCrusaderRallySpeedStatusEffect";

    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(6);

    [DataField]
    public float SpeedMultiplier = 1.25f;
}
