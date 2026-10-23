using Content.Shared.Damage;
using Content.Shared.Trigger.Components.Effects;
using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Trigger;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ReviveOnTriggerComponent : BaseXOnTriggerComponent
{
    [DataField]
    public DamageSpecifier ResidualDamage = new();

    [DataField]
    public float BloodLevel = 400f;

    [DataField]
    public float BleedReduction = 100f;

    [DataField]
    public bool Vomit = true;
}
