using Content.Shared.FixedPoint;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.ADT.Sandevistan;

/// <summary>
/// Маркер для Statveka Sandevistan. Задаёт параметры отличные от стандартного санда.
/// </summary>
[RegisterComponent]
public sealed partial class StatvekaSandevistanImplantComponent : Component
{
    [DataField]
    public float MovementSpeedModifier = 1f;

    [DataField]
    public float AttackSpeedModifier = 1f;

    [DataField]
    public float DoAfterSpeedModifier = 1f;

    [DataField]
    public bool SlowfieldEnabled = true;

    [DataField]
    public float SlowfieldRadius = 7f;

    [DataField]
    public float SlowfieldMobMultiplier = 0.15f;

    [DataField]
    public SortedDictionary<SandevistanState, FixedPoint2> Thresholds = new()
    {
        { SandevistanState.Warning,  6  },
        { SandevistanState.Shaking,  10 },
        { SandevistanState.Damage,   20 },
        { SandevistanState.Disable,  24 },
    };
}
