using Content.Shared.Damage;
using Content.Shared.ADT.Spreader;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.ADT.Spreader;

public sealed class SupermatterKudzuSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    [ValidatePrototypeId<EdgeSupermatterSpreaderPrototype>]
    private const string SupermatterKudzuGroup = "SupermatterKudzu";

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<SupermatterKudzuComponent, ComponentStartup>(SetupKudzu);
        SubscribeLocalEvent<SupermatterKudzuComponent, SupermatterSpreadNeighborsEvent>(OnKudzuSpread);
    }

    private void OnKudzuSpread(EntityUid uid, SupermatterKudzuComponent component, ref SupermatterSpreadNeighborsEvent args)
    {
        if (component.GrowthLevel < 3)
            return;

        if (args.NeighborFreeTiles.Count == 0)
        {
            RemCompDeferred<ActiveEdgeSupermatterSpreaderComponent>(uid);
            return;
        }

        if (!_robustRandom.Prob(component.SpreadChance))
            return;

        var prototype = MetaData(uid).EntityPrototype?.ID;

        if (prototype == null)
        {
            RemCompDeferred<ActiveEdgeSupermatterSpreaderComponent>(uid);
            return;
        }

        foreach (var neighbor in args.NeighborFreeTiles)
        {
            var neighborUid = Spawn(prototype, _map.GridTileToLocal(neighbor.Tile.GridUid, neighbor.Grid, neighbor.Tile.GridIndices));
            DebugTools.Assert(HasComp<EdgeSupermatterSpreaderComponent>(neighborUid));
            DebugTools.Assert(HasComp<ActiveEdgeSupermatterSpreaderComponent>(neighborUid));
            DebugTools.Assert(Comp<EdgeSupermatterSpreaderComponent>(neighborUid).Id == SupermatterKudzuGroup);
            args.Updates--;
            if (args.Updates <= 0)
                return;
        }
    }

    private void SetupKudzu(EntityUid uid, SupermatterKudzuComponent component, ComponentStartup args)
    {
        if (!EntityManager.TryGetComponent<AppearanceComponent>(uid, out var appearance))
        {
            return;
        }

        _appearance.SetData(uid, SupermatterKudzuVisuals.Variant, _robustRandom.Next(1, component.SpriteVariants), appearance);
        _appearance.SetData(uid, SupermatterKudzuVisuals.GrowthLevel, 1, appearance);
    }

    /// <inheritdoc/>
    public override void Update(float frameTime)
    {
        var appearanceQuery = GetEntityQuery<AppearanceComponent>();
        var query = EntityQueryEnumerator<GrowingSupermatterKudzuComponent>();
        var kudzuQuery = GetEntityQuery<SupermatterKudzuComponent>();
        var curTime = _timing.CurTime;

        while (query.MoveNext(out var uid, out var grow))
        {
            if (grow.NextTick > curTime)
                continue;

            grow.NextTick = curTime + TimeSpan.FromSeconds(0.5);

            if (!kudzuQuery.TryGetComponent(uid, out var kudzu))
            {
                RemCompDeferred(uid, grow);
                continue;
            }

            if (!_robustRandom.Prob(kudzu.GrowthTickChance))
            {
                continue;
            }

            kudzu.GrowthLevel += 1;

            if (kudzu.GrowthLevel >= 3)
            {
                // why cache when you can simply cease to be? Also saves a bit of memory/time.
                RemCompDeferred(uid, grow);
            }

            if (appearanceQuery.TryGetComponent(uid, out var appearance))
            {
                _appearance.SetData(uid, SupermatterKudzuVisuals.GrowthLevel, kudzu.GrowthLevel, appearance);
            }
        }
    }
}
