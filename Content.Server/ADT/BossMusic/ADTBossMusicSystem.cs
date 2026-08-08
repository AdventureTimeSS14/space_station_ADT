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
            ClearListeners((uid, comp));
            return;
        }

        foreach (var listener in comp.Listeners.Keys)
        {
            if (!TryComp<ADTBossMusicListenerComponent>(listener, out var listenerComp))
                continue;

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
        ClearListeners((uid, comp));
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
        ClearListeners(ent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_timing.CurTime < _nextUpdate)
            return;

        _nextUpdate = _timing.CurTime + UpdateInterval;

        var query = EntityQueryEnumerator<ADTBossMusicComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            UpdateBoss((uid, comp));
        }
    }

    private void UpdateBoss(Entity<ADTBossMusicComponent> ent)
    {
        if (TerminatingOrDeleted(ent.Owner) || _mobState.IsDead(ent.Owner))
        {
            ClearListeners(ent);
            return;
        }

        if (ent.Comp.Music == null)
        {
            ClearListeners(ent);
            return;
        }

        if (HasLivingTarget(ent.Owner))
            StartCombat(ent.Owner, ent.Comp);

        if (!IsInCombat(ent.Owner, ent.Comp))
        {
            ClearListeners(ent);
            return;
        }

        var bossCoords = _transform.GetMapCoordinates(ent.Owner);
        var rangeSquared = ent.Comp.Range * ent.Comp.Range;
        var exitRangeSquared = ent.Comp.ExitRange * ent.Comp.ExitRange;
        var now = _timing.CurTime;

        _toRemove.Clear();

        foreach (var (listener, leftAt) in ent.Comp.Listeners)
        {
            if (!IsValidListener(listener))
            {
                _toRemove.Add(listener);
                continue;
            }

            var coords = _transform.GetMapCoordinates(listener);

            if (coords.MapId == bossCoords.MapId
                && (coords.Position - bossCoords.Position).LengthSquared() <= exitRangeSquared)
            {
                ent.Comp.Listeners[listener] = null;
                continue;
            }

            if (leftAt == null)
            {
                ent.Comp.Listeners[listener] = now;
                continue;
            }

            if (now - leftAt.Value >= ent.Comp.ExitDelay)
                _toRemove.Add(listener);
        }

        foreach (var listener in _toRemove)
        {
            RemoveListener(ent, listener);
        }

        foreach (var session in _player.Sessions)
        {
            if (session.AttachedEntity is not { } player)
                continue;

            if (ent.Comp.Listeners.ContainsKey(player))
                continue;

            if (!IsValidListener(player))
                continue;

            var coords = _transform.GetMapCoordinates(player);

            if (coords.MapId != bossCoords.MapId)
                continue;

            if ((coords.Position - bossCoords.Position).LengthSquared() > rangeSquared)
                continue;

            AddListener(ent, player);
        }
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

        return !TryComp<MobStateComponent>(uid, out var mobState) || _mobState.IsAlive(uid, mobState);
    }

    private void AddListener(Entity<ADTBossMusicComponent> ent, EntityUid player)
    {
        if (ent.Comp.Music is not { } music)
            return;

        if (TryComp<ADTBossMusicListenerComponent>(player, out var existing)
            && existing.Boss is { } other
            && other != ent.Owner
            && !TerminatingOrDeleted(other))
            return;

        var listener = EnsureComp<ADTBossMusicListenerComponent>(player);
        listener.Boss = ent.Owner;
        listener.Music = music;
        Dirty(player, listener);

        ent.Comp.Listeners[player] = null;
    }

    private void RemoveListener(Entity<ADTBossMusicComponent> ent, EntityUid player)
    {
        ent.Comp.Listeners.Remove(player);

        if (!TryComp<ADTBossMusicListenerComponent>(player, out var listener))
            return;

        if (listener.Boss != ent.Owner)
            return;

        RemCompDeferred<ADTBossMusicListenerComponent>(player);
    }

    private void ClearListeners(Entity<ADTBossMusicComponent> ent)
    {
        if (ent.Comp.Listeners.Count == 0)
            return;

        var listeners = new List<EntityUid>(ent.Comp.Listeners.Keys);
        foreach (var listener in listeners)
        {
            RemoveListener(ent, listener);
        }
    }
}
