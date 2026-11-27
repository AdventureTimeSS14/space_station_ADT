using Content.Shared.ADT.Silicons.Borgs.Components;
using Content.Shared.Alert;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Follower;
using Content.Shared.Popups;
using Content.Shared.Power.EntitySystems;
using Content.Shared.PowerCell;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Silicons.Borgs;

public sealed class ADTBorgSelfRepairSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private AlertsSystem _alerts = default!;
    [Dependency] private DamageableSystem _damage = default!;
    [Dependency] private FollowerSystem _follower = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private PowerCellSystem _powerCell = default!;

    public static readonly ProtoId<AlertPrototype> SelfRepairAlert = "ADTBorgSelfRepair";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTBorgSelfRepairComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<ADTBorgSelfRepairComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<ADTBorgSelfRepairComponent, ADTBorgSelfRepairToggleAlertEvent>(OnAlertToggle);
    }

    private void OnStartup(Entity<ADTBorgSelfRepairComponent> ent, ref ComponentStartup args)
    {
        EnsureComp<AlertsComponent>(ent.Owner);
        _alerts.ShowAlert(ent.Owner, SelfRepairAlert);
    }

    private void OnShutdown(Entity<ADTBorgSelfRepairComponent> ent, ref ComponentShutdown args)
    {
        _alerts.ClearAlert(ent.Owner, SelfRepairAlert);
        Recall(ent);
    }

    private void OnAlertToggle(Entity<ADTBorgSelfRepairComponent> ent, ref ADTBorgSelfRepairToggleAlertEvent args)
    {
        args.Handled = true;

        var curTime = _timing.CurTime;
        if (curTime < ent.Comp.NextToggleTime)
        {
            _popup.PopupEntity(Loc.GetString("adt-borg-selfrepair-cooldown"), ent.Owner, ent.Owner);
            return;
        }
        ent.Comp.NextToggleTime = curTime + TimeSpan.FromSeconds(ent.Comp.ToggleCooldown);

        if (ent.Comp.Released)
            Recall(ent);
        else
            Release(ent);

        _alerts.UpdateAlert(ent.Owner, SelfRepairAlert, cooldown: ent.Comp.NextToggleTime);
    }

    private void Release(Entity<ADTBorgSelfRepairComponent> ent)
    {
        if (ent.Comp.Released || ent.Comp.Wisp is { } existing && !TerminatingOrDeleted(existing))
            return;

        ent.Comp.Released = true;

        var orb = Spawn(ent.Comp.WispProto, Transform(ent.Owner).Coordinates);
        _follower.StartFollowingEntity(orb, ent.Owner);
        ent.Comp.Wisp = orb;

        var active = EnsureComp<ADTBorgSelfRepairActiveComponent>(ent.Owner);
        active.NextHealTime = _timing.CurTime + TimeSpan.FromSeconds(ent.Comp.HealInterval);

        _popup.PopupEntity(Loc.GetString("adt-borg-selfrepair-release"), ent.Owner, ent.Owner);
    }

    private void Recall(Entity<ADTBorgSelfRepairComponent> ent)
    {
        if (!ent.Comp.Released)
            return;

        if (ent.Comp.Wisp is { } wisp && !TerminatingOrDeleted(wisp))
            QueueDel(wisp);

        ent.Comp.Wisp = null;
        ent.Comp.Released = false;
        RemCompDeferred<ADTBorgSelfRepairActiveComponent>(ent.Owner);

        _popup.PopupEntity(Loc.GetString("adt-borg-selfrepair-return"), ent.Owner, ent.Owner);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;
        var query = EntityQueryEnumerator<ADTBorgSelfRepairActiveComponent, ADTBorgSelfRepairComponent>();
        while (query.MoveNext(out var uid, out var active, out var comp))
        {
            if (curTime < active.NextHealTime)
                continue;

            active.NextHealTime = curTime + TimeSpan.FromSeconds(comp.HealInterval);
            DoHealTick((uid, comp));
        }
    }

    private void DoHealTick(Entity<ADTBorgSelfRepairComponent> ent)
    {
        if (!TryComp<DamageableComponent>(ent.Owner, out var damageable))
            return;

        if (!_damage.TryGetDamageGreaterThan((ent.Owner, damageable), FixedPoint2.Zero, out _))
            return;

        if (!_powerCell.TryUseCharge(ent.Owner, ent.Comp.EnergyCost))
            return;

        _damage.HealDistributed((ent.Owner, damageable), -ent.Comp.HealAmount, origin: ent.Comp.Wisp);
    }
}
