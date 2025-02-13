using Content.Shared.Damage;

namespace Content.Shared.ADT.Combat;

[Serializable, ImplicitDataDefinitionForInheritors]
public abstract partial class BaseComboEvent : EntityEventArgs
{
    public EntityUid? User = null;
    public EntityUid? Target = null;
}
public sealed partial class ComboTargetDamageEvent : BaseComboEvent
{
    [DataField(required: true)]
    public DamageSpecifier Damage = default!;
    [DataField]
    public bool IgnoreResistances = false;
}
public sealed partial class ComboUserDamageEvent : BaseComboEvent
{
    [DataField(required: true)]
    public DamageSpecifier Damage = default!;
    [DataField]
    public bool IgnoreResistances = false;
}
public sealed partial class ComboStunEvent : BaseComboEvent
{
    [DataField(required: true)]
    public float StaminaDamage;
}
public sealed partial class ComboTrowEvent : BaseComboEvent
{
}
public sealed partial class ComboSpawnEntityEvent : BaseComboEvent
{
    [DataField(required: true)]
    public string EntityPrototype;
}

public sealed partial class ComboFallEvent : BaseComboEvent
{
}
public sealed partial class ComboAdditionalDamageToStunnedEvent : BaseComboEvent
{
    [DataField(required: true)]
    public DamageSpecifier Damage = default!;
    [DataField]
    public bool IgnoreResistances = false;
}
public sealed partial class ComboDropFromActiveHandEvent : BaseComboEvent
{
}
