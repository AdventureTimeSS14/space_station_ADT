using Content.Shared.ADT.Implants;
using Content.Shared.Damage.Systems;
using Content.Shared.Implants;
using Content.Shared.Trigger;

namespace Content.Shared.ADT.Trigger;

public sealed class TriggerOnDamageThresholdSystem : TriggerOnXSystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly ImplantCritThresholdSystem _crit = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TriggerOnDamageThresholdComponent, ImplantRelayEvent<DamageChangedEvent>>(OnDamageChanged);
    }

    private void OnDamageChanged(Entity<TriggerOnDamageThresholdComponent> ent, ref ImplantRelayEvent<DamageChangedEvent> args)
    {
        var target = args.ImplantedEntity;

        if (!_crit.TryGetFirstCritThreshold(target, out var critThreshold))
            return;

        var threshold = critThreshold.Float() - ent.Comp.MarginBeforeCrit;
        var damage = _damageable.GetTotalDamage(target).Float();

        if (!ent.Comp.WasTriggered && damage >= threshold)
        {
            ent.Comp.WasTriggered = Trigger.Trigger(ent.Owner, target, ent.Comp.KeyOut);
        }
        else if (ent.Comp.WasTriggered && damage < threshold)
        {
            ent.Comp.WasTriggered = false;
        }
    }
}
