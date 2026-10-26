using Content.Shared.ADT.Mining;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Item;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Mining;

public sealed class ADTScavengerSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private static readonly SoundSpecifier EatSound = new SoundCollectionSpecifier("eating");

    public override void Initialize()
    {
        SubscribeLocalEvent<ADTScavengerComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<ADTScavengerComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnInit(Entity<ADTScavengerComponent> ent, ref ComponentInit args)
    {
        _container.EnsureContainer<Container>(ent, ent.Comp.ContainerId);
    }

    public override void Update(float frameTime)
    {
        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<ADTScavengerComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (now < comp.NextEat)
                continue;

            comp.NextEat = now + comp.Interval;

            if (_mobState.IsDead(uid))
                continue;

            TryEat((uid, comp));
        }
    }

    private void TryEat(Entity<ADTScavengerComponent> ent)
    {
        if (!_container.TryGetContainer(ent, ent.Comp.ContainerId, out var belly))
            return;

        if (belly.ContainedEntities.Count >= ent.Comp.Capacity)
            return;

        var coords = _transform.GetMapCoordinates(ent.Owner);
        var nearby = new HashSet<Entity<ItemComponent>>();
        _lookup.GetEntitiesInRange(coords, ent.Comp.Range, nearby);

        foreach (var item in nearby)
        {
            if (TerminatingOrDeleted(item) || _container.IsEntityInContainer(item))
                continue;

            if (_whitelist.IsWhitelistFail(ent.Comp.Whitelist, item))
                continue;

            if (ent.Comp.StoreEaten)
            {
                if (!_container.Insert(item.Owner, belly))
                    continue;
            }
            else
            {
                QueueDel(item);
            }

            if (ent.Comp.HealOnEat is { } heal)
                _damageable.TryChangeDamage(ent.Owner, heal);

            _audio.PlayPvs(EatSound, ent.Owner);
            return;
        }
    }

    private void OnMobStateChanged(Entity<ADTScavengerComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead)
            return;

        if (_container.TryGetContainer(ent, ent.Comp.ContainerId, out var belly))
            _container.EmptyContainer(belly);
    }
}
