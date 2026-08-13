using System.Numerics;
using Content.Server.Chat.Systems;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Events;
using Content.Shared.ADT.Salvage.Components;
using Content.Shared.Weapons.Melee.Components;
using Robust.Shared.Map;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Salvage.Systems;

public sealed partial class MegafaunaSystem : EntitySystem
{
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<MegafaunaComponent, AttemptMeleeThrowOnHitEvent>(OnAttemptMeleeThrowOnHit);

        SubscribeLocalEvent<MegafaunaComponent, MapInitEvent>(OnMegafaunaMapInit);
        SubscribeLocalEvent<ADTLavalandBoundComponent, EntParentChangedMessage>(OnBoundParentChanged);
        //TODO: SubscribeLocalEvent<ShuttleComponent, FTLStartedEvent>(OnShuttleFTL);

        InitializeDrake();
        InitializeAggro();
    }

    private void OnAttemptMeleeThrowOnHit(Entity<MegafaunaComponent> _, ref AttemptMeleeThrowOnHitEvent args)
    {
        args.Cancelled = true;
    }

    private void OnMegafaunaMapInit(Entity<MegafaunaComponent> ent, ref MapInitEvent args)
    {
        var bound = EnsureComp<ADTLavalandBoundComponent>(ent);
        if (Transform(ent).MapUid is { } map)
            bound.HomeMap = map;
    }

    private void OnBoundParentChanged(Entity<ADTLavalandBoundComponent> ent, ref EntParentChangedMessage args)
    {
        var xform = Transform(ent);

        if (!Exists(ent.Comp.HomeMap) && xform.MapUid is { } currentMap)
            ent.Comp.HomeMap = currentMap;

        if (!Exists(ent.Comp.HomeMap))
            return;

        if (xform.GridUid is not { } grid || !HasComp<ShuttleComponent>(grid))
            return;

        var worldPos = _transform.GetMapCoordinates(ent.Owner, xform).Position;
        SendHome(ent, worldPos);
    }

    private void OnShuttleFTL(Entity<ShuttleComponent> ent, ref FTLStartedEvent args)
    {
        var found = new List<Entity<ADTLavalandBoundComponent>>();
        FindBound(ent.Owner, found);

        foreach (var boss in found)
        {
            var localPos = Transform(boss.Owner).LocalPosition;
            SendHome(boss, Vector2.Transform(localPos, args.FTLFrom));
        }
    }

    private void FindBound(EntityUid uid, List<Entity<ADTLavalandBoundComponent>> found)
    {
        if (TryComp<ADTLavalandBoundComponent>(uid, out var bound))
        {
            found.Add((uid, bound));
            return;
        }

        var children = Transform(uid).ChildEnumerator;
        while (children.MoveNext(out var child))
        {
            FindBound(child, found);
        }
    }

    private void SendHome(Entity<ADTLavalandBoundComponent> ent, Vector2 position)
    {
        if (!Exists(ent.Comp.HomeMap))
            return;

        var homeMapId = Transform(ent.Comp.HomeMap).MapID;
        _transform.SetMapCoordinates(ent.Owner, new MapCoordinates(position, homeMapId));
    }
}
