using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.ReliveResuscitation;

/// <summary>
/// Компонент, позволяющий проводить сердечно-лёгочную реанимацию сущности в критическом состоянии.
/// Убирает удушение взамен на добавление грубого урона.
/// by Шрёдька <3 (Schrodinger71)
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ReliveResuscitationComponent : Component
{
    /// <summary>
    /// Время проведения реанимации в секундах
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("delay")]
    public float Delay = 3.3f;

    [DataField]
    public FixedPoint2 _asphyxiationHeal = -20;

    [DataField]
    public FixedPoint2 _bluntDamage = 3;

    [DataField]
    public int MinAsphyxiationHeal = -13;

    [DataField]
    public int MinBluntDamage = 7;

    [DataField]
    public int MaxAsphyxiationHeal = -17;

    [DataField]
    public int MaxBluntDamage = 13;

    [DataField]
    public ProtoId<DamageTypePrototype> DamageAsphyxiation = "Asphyxiation";

    [DataField]
    public ProtoId<DamageTypePrototype> DamageBlunt = "Blunt";
}

