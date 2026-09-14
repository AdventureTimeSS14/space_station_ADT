using Content.Shared.ADT.Legion;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Damage.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Systems;
using Content.Shared.Popups;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Legion;

public sealed class ADTLegionSkullSystem : EntitySystem
{
    [Dependency] private readonly ADTLegionSystem _legion = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly MobThresholdSystem _thresholds = default!;
    [Dependency] private readonly NpcFactionSystem _faction = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTLegionSkullComponent, MeleeHitEvent>(OnSkullHit);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<ADTLegionTrackingComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            if (now < comp.EndTime)
                continue;

            RemCompDeferred<ADTLegionTrackingComponent>(uid);
        }
    }

    private void OnSkullHit(Entity<ADTLegionSkullComponent> ent, ref MeleeHitEvent args)
    {
        if (args.Handled || args.HitEntities.Count == 0)
            return;

        var target = args.HitEntities[0];

        if (!HasComp<MobStateComponent>(target))
            return;

        if (!TryJoinSwarm(ent, target))
        {
            args.Handled = true;
            return;
        }

        if (!CanInfest(ent, target))
            return;

        args.Handled = true;
        Infest(ent, target);
    }

    private bool TryJoinSwarm(Entity<ADTLegionSkullComponent> ent, EntityUid target)
    {
        var now = _timing.CurTime;
        var tracked = TryComp<ADTLegionTrackingComponent>(target, out var tracker);

        if (tracked && now > tracker!.EndTime)
        {
            tracker.Skulls.Clear();
            tracked = false;
        }

        tracker ??= EnsureComp<ADTLegionTrackingComponent>(target);
        tracker.EndTime = now + tracker.Duration;

        if (tracked && (tracker.Skulls.Contains(ent.Owner) || tracker.Skulls.Count >= ent.Comp.SwarmThreshold))
            return true;

        tracker.Skulls.Add(ent.Owner);
        return false;
    }

    private bool CanInfest(Entity<ADTLegionSkullComponent> ent, EntityUid target)
    {
        if (!HasComp<HumanoidProfileComponent>(target))
            return false;

        if (_mobState.IsCritical(target))
            return true;

        return ent.Comp.CanInfestDead && _mobState.IsDead(target);
    }

    private void Infest(Entity<ADTLegionSkullComponent> ent, EntityUid target)
    {
        if (!TryKill(target, ent.Owner))
            return;

        var coords = _transform.GetMoverCoordinates(target);

        _popup.PopupCoordinates(
            Loc.GetString(ent.Comp.InfestMessage, ("target", Identity.Entity(target, EntityManager))),
            coords,
            PopupType.LargeCaution);

        var legion = Spawn(ent.Comp.LegionProto, coords);

        if (TryComp<NpcFactionMemberComponent>(ent.Owner, out var ourFaction))
        {
            _faction.ClearFactions(legion, false);
            _faction.AddFactions(legion, ourFaction.Factions);
        }

        if (TryComp<ADTLegionComponent>(legion, out var legionComp))
            _legion.TryStoreBody((legion, legionComp), target);

        QueueDel(ent.Owner);
    }

    private bool TryKill(EntityUid target, EntityUid origin)
    {
        if (!TryComp<DamageableComponent>(target, out var damageable))
            return false;

        if (!_thresholds.TryGetDeadThreshold(target, out var deadThreshold))
            return false;

        var missing = deadThreshold.Value - damageable.TotalDamage;

        if (missing > FixedPoint2.Zero)
        {
            var damage = new DamageSpecifier();
            damage.DamageDict.Add("Blunt", missing);
            _damageable.TryChangeDamage((target, damageable), damage, true, origin: origin);
        }

        return _mobState.IsDead(target);
    }
}
