using System.Numerics;
using Content.Shared.ADT.Bubblegum;
using Content.Shared.ADT.Bubblegum.Abilities;
using Content.Shared.Maps;
using Content.Shared.Physics;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Server.ADT.Bubblegum.Abilities;

public sealed class BubblegumSummonNarsiSystem : EntitySystem
{
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly TurfSystem _turf = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BubblegumSummonNarsiComponent, BubblegumSummonNarsiActionEvent>(OnAction);
    }

    private void OnAction(Entity<BubblegumSummonNarsiComponent> ent, ref BubblegumSummonNarsiActionEvent args)
    {
        if (args.Handled)
            return;

        var target = _transform.ToMapCoordinates(args.Target);
        if (target.MapId == MapId.Nullspace)
            return;

        args.Handled = true;
        TrySummon(ent, target);
    }

    public void TrySummon(Entity<BubblegumSummonNarsiComponent> ent, MapCoordinates target)
    {
        if (!_mapManager.TryFindGridAt(target, out var gridUid, out var grid))
            return;

        var spawned = 0;
        var attempts = 0;
        const int maxAttempts = 30;

        while (spawned < ent.Comp.Count && attempts < maxAttempts)
        {
            attempts++;

            var ang = _random.NextDouble() * Math.Tau;
            var radius = ent.Comp.MinDistance + (float)_random.NextDouble() * (ent.Comp.SearchRange - ent.Comp.MinDistance);
            var offset = new Vector2((float)Math.Cos(ang), (float)Math.Sin(ang)) * radius;
            var pos = new MapCoordinates(target.Position + offset, target.MapId);

            var entityCoords = _transform.ToCoordinates((gridUid, null), pos);
            var tile = _turf.GetTileRef(entityCoords);
            if (tile == null)
                continue;
            if (_turf.IsTileBlocked(tile.Value, CollisionGroup.MobMask))
                continue;

            var minion = Spawn(ent.Comp.MinionPrototype, entityCoords);
            EnsureComp<BubblegumMinionComponent>(minion).Summoner = ent.Owner;
            spawned++;
        }
    }
}
