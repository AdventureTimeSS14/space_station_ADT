using Content.Shared.ADT.Bubblegum;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Gibbing;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Weapons.Melee.Events;
using Content.Server.NPC.HTN;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Bubblegum;

public sealed class BubblegumDevourSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly MobThresholdSystem _thresholds = default!;
    [Dependency] private readonly GibbingSystem _gibbing = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly HTNSystem _htn = default!;

    private static readonly TimeSpan PostHitDevourDelay = TimeSpan.FromSeconds(0.5);

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BubblegumComponent, MeleeHitEvent>(OnMeleeHit);
    }

    private void OnMeleeHit(Entity<BubblegumComponent> ent, ref MeleeHitEvent args)
    {
        if (!args.IsHit)
            return;

        if (HasComp<BubblegumPendingDevourComponent>(ent))
            return;

        foreach (var target in args.HitEntities)
        {
            if (target == ent.Owner)
                continue;

            if (!HasComp<MobStateComponent>(target))
                continue;

            if (!_mobState.IsDead(target))
                continue;

            ScheduleDevour(ent, target, PostHitDevourDelay);
            break;
        }
    }

    private void ScheduleDevour(EntityUid boss, EntityUid target, TimeSpan delay)
    {
        var devour = EnsureComp<BubblegumPendingDevourComponent>(boss);
        devour.Target = target;
        devour.ExecuteAt = _timing.CurTime + delay;
        Dirty(boss, devour);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<BubblegumPendingDevourComponent>();
        while (query.MoveNext(out var uid, out var devour))
        {
            if (now < devour.ExecuteAt)
                continue;

            var target = devour.Target;
            RemCompDeferred<BubblegumPendingDevourComponent>(uid);

            if (TerminatingOrDeleted(target))
                continue;

            if (!HasComp<MobStateComponent>(target))
                continue;

            Devour(uid, target);
        }
    }

    private void Devour(EntityUid boss, EntityUid target)
    {
        if (TryGetMaxHealth(target, out var maxHp))
        {
            var heal = new DamageSpecifier();
            heal.DamageDict.Add("Blunt", -maxHp / 2f);
            _damageable.TryChangeDamage(boss, heal, true, origin: boss);
        }

        _popup.PopupEntity(Loc.GetString("bubblegum-devour-popup", ("target", target)), boss, PopupType.LargeCaution);

        var gibs = _gibbing.Gib(target, true, boss);
        if (gibs.Count == 0)
            QueueDel(target);

        if (TryComp<HTNComponent>(boss, out var htn)
            && htn.Blackboard.TryGetValue<EntityUid>("Target", out var currentTarget, EntityManager)
            && currentTarget == target)
        {
            htn.Blackboard.Remove<EntityUid>("Target");
            _htn.Replan(htn);
        }
    }

    private bool TryGetMaxHealth(EntityUid uid, out float max)
    {
        max = 0f;
        if (!TryComp<MobThresholdsComponent>(uid, out var thresholds))
            return false;

        foreach (var (damage, _) in thresholds.Thresholds)
        {
            var d = (float)damage;
            if (d > max)
                max = d;
        }

        return max > 0f;
    }
}
