using Content.Shared.Damage;
using Content.Shared.Trigger.Components.Effects;
using Content.Shared.Whitelist; // ADT-Tweak

namespace Content.Shared.Trigger.Systems;

public sealed partial class DamageOnTriggerSystem : XOnTriggerSystem<DamageOnTriggerComponent>
{
    [Dependency] private Damage.Systems.DamageableSystem _damageableSystem = default!;
    [Dependency] private EntityWhitelistSystem _entityWhitelist = default!; // ADT-Tweak

    protected override void OnTrigger(Entity<DamageOnTriggerComponent> ent, EntityUid target, ref TriggerEvent args)
    {
        // ADT-Tweak-Start
        if (_entityWhitelist.IsWhitelistPass(ent.Comp.Blacklist, target))
            return;
        // ADT-Tweak-End

        var damage = new DamageSpecifier(ent.Comp.Damage);
        var ev = new BeforeDamageOnTriggerEvent(damage, target);
        RaiseLocalEvent(ent.Owner, ref ev);

        args.Handled |= _damageableSystem.TryChangeDamage(target, ev.Damage, ent.Comp.IgnoreResistances, origin: ent.Owner);
    }
}

/// <summary>
/// Raised on an entity before it deals damage using DamageOnTriggerComponent.
/// Used to modify the damage that will be dealt.
/// </summary>
[ByRefEvent]
public record struct BeforeDamageOnTriggerEvent(DamageSpecifier Damage, EntityUid Tripper);
