using Content.Server.NPC.HTN;
using Content.Shared.ADT.BossMusic;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Ghost;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.ADT.BossMusic;

public sealed class ADTBossMusicSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private const string TargetKey = "Target";

    private static readonly TimeSpan UpdateInterval = TimeSpan.FromSeconds(0.5);

    private TimeSpan _nextUpdate;

    private readonly List<Entity<ADTBossMusicComponent>> _activeBosses = new();

    private readonly List<EntityUid> _toRemove = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTBossMusicComponent, DamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<ADTBossMusicComponent, MeleeHitEvent>(OnMeleeHit);
        SubscribeLocalEvent<ADTBossMusicComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<ADTBossMusicComponent, ComponentShutdown>(OnBossShutdown);
    }

    public void SetMusic(EntityUid uid, ProtoId<ADTBossMusicPrototype>? music, ADTBossMusicComponent? comp = null)
    {
        if (!Resolve(uid, ref comp, false))
            return;

        if (comp.Music == music)
            return;

        comp.Music = music;

        if (music is not { } newMusic)
        {
            DropListeners(uid);
            return;
        }

        var query = EntityQueryEnumerator<ADTBossMusicListenerComponent>();

        while (query.MoveNext(out var listener, out var listenerComp))
        {
            if (listenerComp.Boss != uid)
                continue;

            listenerComp.Music = newMusic;
            Dirty(listener, listenerComp);
        }
    }

    public void StartCombat(EntityUid uid, ADTBossMusicComponent? comp = null)
    {
        if (!Resolve(uid, ref comp, false))
            return;

        comp.CombatUntil = _timing.CurTime + comp.CombatTimeout;
    }

    public void StopCombat(EntityUid uid, ADTBossMusicComponent? comp = null)
    {
        if (!Resolve(uid, ref comp, false))
            return;

        comp.CombatUntil = TimeSpan.Zero;
        DropListeners(uid);
    }

    public bool IsInCombat(EntityUid uid, ADTBossMusicComponent? comp = null)
    {
        if (!Resolve(uid, ref comp, false))
            return false;

        return _timing.CurTime < comp.CombatUntil;
    }

    private void OnDamageChanged(Entity<ADTBossMusicComponent> ent, ref DamageChangedEvent args)
    {
        if (args.DamageDelta == null || args.DamageDelta.GetTotal() <= FixedPoint2.Zero)
            return;

        StartCombat(ent.Owner, ent.Comp);
    }

    private void OnMeleeHit(Entity<ADTBossMusicComponent> ent, ref MeleeHitEvent args)
    {
        if (args.HitEntities.Count == 0)
            return;

        StartCombat(ent.Owner, ent.Comp);
    }

    private void OnMobStateChanged(Entity<ADTBossMusicComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead)
            return;

        StopCombat(ent.Owner, ent.Comp);
    }

    private void OnBossShutdown(Entity<ADTBossMusicComponent> ent, ref ComponentShutdown args)
    {
        DropListeners(ent.Owner);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_timing.CurTime < _nextUpdate)
            return;

        _nextUpdate = _timing.CurTime + UpdateInterval;

        CollectActiveBosses();

        if (_activeBosses.Count > 0)
        {
            foreach (var session in _player.Sessions)
            {
                if (session.AttachedEntity is not { } player)
                    continue;

                UpdateListener(player, _timing.CurTime);
            }
        }

        DropOrphanedListeners();
    }

    private void CollectActiveBosses()
    {
        _activeBosses.Clear();

        var query = EntityQueryEnumerator<ADTBossMusicComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            if (TerminatingOrDeleted(uid) || _mobState.IsDead(uid))
                continue;

            if (comp.Music == null)
                continue;

            if (HasLivingTarget(uid))
                StartCombat(uid, comp);

            if (!IsInCombat(uid, comp))
                continue;

            _activeBosses.Add((uid, comp));
        }
    }

    private void UpdateListener(EntityUid player, TimeSpan now)
    {
        TryComp<ADTBossMusicListenerComponent>(player, out var listener);

        if (!IsValidListener(player))
        {
            ClearListener(player);
            return;
        }

        var coords = _transform.GetMapCoordinates(player);
        var current = GetActiveBoss(listener?.Boss);

        if (current is { } stay && InRange(stay, coords, stay.Comp.ExitRange))
        {
            Bind(player, stay, listener);
            return;
        }

        if (FindBoss(coords) is { } nearest)
        {
            Bind(player, nearest, listener);
            return;
        }

        if (listener == null)
            return;

        if (current is not { } leaving)
        {
            ClearListener(player);
            return;
        }

        listener.LeftAt ??= now;

        if (now - listener.LeftAt.Value >= leaving.Comp.ExitDelay)
            ClearListener(player);
    }

    private void Bind(EntityUid player, Entity<ADTBossMusicComponent> boss, ADTBossMusicListenerComponent? listener)
    {
        if (boss.Comp.Music is not { } music)
            return;

        listener ??= EnsureComp<ADTBossMusicListenerComponent>(player);
        listener.LeftAt = null;

        if (listener.Boss == boss.Owner && listener.Music == music)
            return;

        listener.Boss = boss.Owner;
        listener.Music = music;
        Dirty(player, listener);
    }

    private Entity<ADTBossMusicComponent>? FindBoss(MapCoordinates coords)
    {
        Entity<ADTBossMusicComponent>? best = null;
        var bestDistance = float.MaxValue;

        foreach (var boss in _activeBosses)
        {
            var bossCoords = _transform.GetMapCoordinates(boss.Owner);

            if (bossCoords.MapId != coords.MapId)
                continue;

            var distance = (coords.Position - bossCoords.Position).LengthSquared();

            if (distance > boss.Comp.Range * boss.Comp.Range)
                continue;

            if (distance >= bestDistance)
                continue;

            bestDistance = distance;
            best = boss;
        }

        return best;
    }

    private Entity<ADTBossMusicComponent>? GetActiveBoss(EntityUid? uid)
    {
        if (uid is not { } boss)
            return null;

        foreach (var active in _activeBosses)
        {
            if (active.Owner == boss)
                return active;
        }

        return null;
    }

    private bool InRange(Entity<ADTBossMusicComponent> boss, MapCoordinates coords, float range)
    {
        var bossCoords = _transform.GetMapCoordinates(boss.Owner);

        if (bossCoords.MapId != coords.MapId)
            return false;

        return (coords.Position - bossCoords.Position).LengthSquared() <= range * range;
    }

    private bool HasLivingTarget(EntityUid uid)
    {
        if (!TryComp<HTNComponent>(uid, out var htn))
            return false;

        if (!htn.Blackboard.TryGetValue<EntityUid>(TargetKey, out var target, EntityManager))
            return false;

        if (TerminatingOrDeleted(target))
            return false;

        return !HasComp<MobStateComponent>(target) || _mobState.IsAlive(target);
    }

    private bool IsValidListener(EntityUid uid)
    {
        if (TerminatingOrDeleted(uid))
            return false;

        if (HasComp<GhostComponent>(uid))
            return false;

        return !TryComp<MobStateComponent>(uid, out var mobState) || !_mobState.IsDead(uid, mobState);
    }

    private void DropOrphanedListeners()
    {
        _toRemove.Clear();

        var query = EntityQueryEnumerator<ADTBossMusicListenerComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            if (GetActiveBoss(comp.Boss) is not null && HasComp<ActorComponent>(uid) && IsValidListener(uid))
                continue;

            _toRemove.Add(uid);
        }

        FlushRemovals();
    }

    private void DropListeners(EntityUid boss)
    {
        _toRemove.Clear();

        var query = EntityQueryEnumerator<ADTBossMusicListenerComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.Boss != boss)
                continue;

            _toRemove.Add(uid);
        }

        FlushRemovals();
    }

    private void FlushRemovals()
    {
        foreach (var uid in _toRemove)
        {
            ClearListener(uid);
        }

        _toRemove.Clear();
    }

    private void ClearListener(EntityUid uid)
    {
        if (TerminatingOrDeleted(uid))
            return;

        RemComp<ADTBossMusicListenerComponent>(uid);
    }
}
