// Ported from RMC-14 (https://github.com/RMC-14/RMC-14), MIT License
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs.Systems;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Content.Shared.Whitelist;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;

namespace Content.Shared._RMC14.OnCollide;

public sealed class SharedRMCOnCollideSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;
    [Dependency] private readonly INetManager _net = default!;

    private EntityQuery<CollideChainComponent> _collideChainQuery;
    private EntityQuery<RMCDamageOnCollideComponent> _damageOnCollideQuery;

    private readonly List<EntityUid> _pending = new();

    public override void Initialize()
    {
        _collideChainQuery = GetEntityQuery<CollideChainComponent>();
        _damageOnCollideQuery = GetEntityQuery<RMCDamageOnCollideComponent>();

        SubscribeLocalEvent<RMCDamageOnCollideComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<RMCDamageOnCollideComponent, StartCollideEvent>(OnStartCollide);
        SubscribeLocalEvent<RMCDamageOnCollideComponent, EndCollideEvent>(OnEndCollide);
        SubscribeLocalEvent<RMCDamageOnCollideComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnStartup(Entity<RMCDamageOnCollideComponent> ent, ref ComponentStartup args)
    {
        _pending.Add(ent.Owner);
    }

    private void OnStartCollide(Entity<RMCDamageOnCollideComponent> ent, ref StartCollideEvent args)
    {
        OnCollide(ent, args.OtherEntity);
    }

    private void OnEndCollide(Entity<RMCDamageOnCollideComponent> ent, ref EndCollideEvent args)
    {
        if (!ent.Comp.CanRehit)
            return;

        ent.Comp.Damaged.Remove(args.OtherEntity);
    }

    private void OnShutdown(Entity<RMCDamageOnCollideComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.Chain is not { } chain || TerminatingOrDeleted(chain))
            return;

        CleanupChain(chain, ent.Owner);
    }

    private void OnCollide(Entity<RMCDamageOnCollideComponent> ent, EntityUid other)
    {
        if (ent.Comp.Disabled)
            return;

        if (ent.Comp.Damaged.Contains(other))
            return;

        if (!_whitelist.IsWhitelistPassOrNull(ent.Comp.Whitelist, other))
            return;

        if (!ent.Comp.DamageDead && _mobState.IsDead(other))
            return;

        if (Transform(other).Anchored)
            return;

        var attempt = new RMCDamageCollideAttemptEvent(other, ent.Comp.Fire);
        RaiseLocalEvent(ent, ref attempt);
        if (attempt.Cancelled)
            return;

        ent.Comp.Damaged.Add(other);

        if (ent.Comp.Chain == null || AddToChain(ent.Comp.Chain.Value, other))
            _damageable.TryChangeDamage(other, ent.Comp.Damage, ent.Comp.IgnoreResistances);
        else
            _damageable.TryChangeDamage(other, ent.Comp.ChainDamage, ent.Comp.IgnoreResistances);

        if (ent.Comp.Paralyze > TimeSpan.Zero && !_standing.IsDown(other))
            _stun.TryAddParalyzeDuration(other, ent.Comp.Paralyze);

        var ev = new RMCDamageCollideEvent(other);
        RaiseLocalEvent(ent, ref ev);
    }

    private bool AddToChain(Entity<CollideChainComponent?> chain, EntityUid add)
    {
        if (!_collideChainQuery.Resolve(chain, ref chain.Comp, false))
            return true;

        if (chain.Comp.Hit.Add(add))
            return true;

        return false;
    }

    public Entity<CollideChainComponent> SpawnChain()
    {
        var ent = Spawn(null, MapCoordinates.Nullspace);
        var comp = EnsureComp<CollideChainComponent>(ent);
        return (ent, comp);
    }

    public void SetChain(Entity<RMCDamageOnCollideComponent?> ent, EntityUid chain)
    {
        if (!_damageOnCollideQuery.Resolve(ent, ref ent.Comp, false))
            return;

        if (ent.Comp.Chain is { } oldChain &&
            oldChain != chain &&
            !TerminatingOrDeleted(oldChain))
        {
            CleanupChain(oldChain, ent.Owner);
        }

        ent.Comp.Chain = chain;
    }

    public void CleanupChain(EntityUid? chain, EntityUid? skip = null)
    {
        if (chain == null || TerminatingOrDeleted(chain.Value))
            return;

        if (!HasRemainingChainRefs(chain.Value, skip))
            Del(chain.Value);
    }

    private bool HasRemainingChainRefs(EntityUid chain, EntityUid? skip = null)
    {
        var query = EntityQueryEnumerator<RMCDamageOnCollideComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if ((skip != null && uid == skip) || TerminatingOrDeleted(uid))
                continue;

            if (comp.Chain == chain)
                return true;
        }

        return false;
    }

    public void DisableDamageOnCollide(Entity<RMCDamageOnCollideComponent?> ent)
    {
        if (!_damageOnCollideQuery.Resolve(ent, ref ent.Comp, false))
            return;

        ent.Comp.Disabled = true;
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient || _pending.Count == 0)
            return;

        try
        {
            foreach (var uid in _pending)
            {
                if (!_damageOnCollideQuery.TryComp(uid, out var comp) || comp.InitDamaged)
                    continue;

                comp.InitDamaged = true;

                foreach (var contact in _physics.GetEntitiesIntersectingBody(uid, (int) comp.Collision))
                {
                    OnCollide((uid, comp), contact);
                }
            }
        }
        finally
        {
            _pending.Clear();
        }
    }
}

[ByRefEvent]
public record struct RMCDamageCollideAttemptEvent(EntityUid Target, bool Fire, bool Cancelled = false);
