using Content.Shared.Alert;
using Content.Shared.Damage.Events;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Events;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Traits.Assorted;
public sealed class PainNumbnessStatusEffectSystem : EntitySystem
{
    [Dependency] private readonly MobThresholdSystem _mobThresholdSystem = default!;
    [Dependency] private readonly AlertsSystem _alertsSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<PainNumbnessStatusEffectComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<PainNumbnessStatusEffectComponent, ComponentRemove>(OnComponentRemove);
        SubscribeLocalEvent<PainNumbnessStatusEffectComponent, BeforeForceSayEvent>(OnChangeForceSay);
        SubscribeLocalEvent<PainNumbnessStatusEffectComponent, BeforeAlertSeverityCheckEvent>(OnAlertSeverityCheck);
    }

    private void OnComponentRemove(EntityUid uid, PainNumbnessStatusEffectComponent component, ComponentRemove args)
    {
        if (!HasComp<MobThresholdsComponent>(uid))
            return;

        _mobThresholdSystem.VerifyThresholds(uid);
    }

    private void OnComponentStartup(EntityUid uid, PainNumbnessStatusEffectComponent component, ComponentStartup args)
    {
        if (!HasComp<MobThresholdsComponent>(uid))
            return;

        _mobThresholdSystem.VerifyThresholds(uid);
    }

    private void OnChangeForceSay(Entity<PainNumbnessStatusEffectComponent> ent, ref BeforeForceSayEvent args)
    {
        if (ent.Comp.ForceSayNumbDataset.HasValue)
            args.Prefix = ent.Comp.ForceSayNumbDataset.Value;
    }

    private void OnAlertSeverityCheck(Entity<PainNumbnessStatusEffectComponent> ent, ref BeforeAlertSeverityCheckEvent args)
    {
        if (ent.Comp.HideHealthAlert && args.CurrentAlert == "HumanHealth")
            args.CancelUpdate = true;
    }
}
