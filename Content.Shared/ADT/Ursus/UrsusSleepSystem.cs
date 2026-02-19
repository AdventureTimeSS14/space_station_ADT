using Content.Shared.Actions;
using Content.Shared.ADT.Sleeping;
using Content.Shared.ADT.Ursus;
using Content.Shared.Bed.Sleep;
using Content.Shared.Damage;
using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Damage.Systems;
using Content.Shared.Popups;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared.ADT.Ursus;

public sealed class UrsusSleepSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly SleepingSystem _sleepingSystem = default!;
    [Dependency] private readonly HungerSystem _hunger = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<UrsusSleepComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<UrsusSleepComponent, UrsusSleepEvent>(OnAction);

        SubscribeLocalEvent<UrsusStasisComponent, DamageChangedEvent>(OnDamage);
        SubscribeLocalEvent<UrsusStasisComponent, SleepStateChangedEvent>(OnWake);
        SubscribeLocalEvent<UrsusStasisComponent, WakingAttemptEvent>(OnWakeAttempt);

        SubscribeLocalEvent<UrsusStasisComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<UrsusStasisComponent, SleepExamineAttemptEvent>(OnSleepExamine);
    }

    private void OnMapInit(Entity<UrsusSleepComponent> ent, ref MapInitEvent args)
    {
        if (_net.IsServer)
            _actions.AddAction(ent.Owner, ref ent.Comp.Action, "ActionUrsusSleep");
    }

    private void OnAction(Entity<UrsusSleepComponent> ent, ref UrsusSleepEvent args)
    {
        if (HasComp<SleepingComponent>(ent.Owner))
            return;

        if (TryComp<HungerComponent>(ent.Owner, out var hunger) && hunger.CurrentThreshold is not (HungerThreshold.Overfed or HungerThreshold.Okay))
        {
            _popup.PopupPredicted(Loc.GetString("ursus-sleep-hungry"), null, ent.Owner, ent.Owner);
            return;
        }

        args.Handled = _sleepingSystem.TrySleeping(ent.Owner);
        EnsureComp<UrsusStasisComponent>(ent.Owner);
    }

    private void OnDamage(Entity<UrsusStasisComponent> ent, ref DamageChangedEvent args)
    {
        if (!_net.IsServer)
            return;

        if (!args.DamageIncreased)
            return;

        if (args.DamageDelta == null)
            return;

        foreach (var item in args.DamageDelta.DamageDict)
        {
            if (ent.Comp.StoredDamage.Contains(item.Key))
                ent.Comp.DamageContained += item.Value.Int();
        }

        if (ent.Comp.DamageContained >= ent.Comp.DamageToWake)
            _sleepingSystem.TryWakeWithCooldown(ent.Owner);
    }

    private void OnWake(Entity<UrsusStasisComponent> ent, ref SleepStateChangedEvent args)
    {
        if (args.FellAsleep)
            return;

        RemComp(ent.Owner, ent.Comp);
    }

    private void OnExamined(Entity<UrsusStasisComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("perishable-1", ("target", Identity.Entity(ent.Owner, EntityManager))));
    }

    private void OnSleepExamine(Entity<UrsusStasisComponent> ent, ref SleepExamineAttemptEvent args)
    {
        args.Cancelled = true;
    }

    private void OnWakeAttempt(Entity<UrsusStasisComponent> ent, ref WakingAttemptEvent args)
    {
        if (ent.Comp.DamageContained < ent.Comp.DamageToWake && args.User != ent.Owner)
            args.Cancelled = true;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_net.IsClient)
            return;

        var query = EntityQueryEnumerator<UrsusStasisComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.NextUpdate > _timing.CurTime)
                continue;

            comp.NextUpdate = _timing.CurTime + TimeSpan.FromSeconds(3);
            _damage.TryChangeDamage(uid, comp.Regen, true);
        }
    }
}
