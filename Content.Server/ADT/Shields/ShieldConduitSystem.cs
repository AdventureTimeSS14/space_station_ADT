using Content.Shared.ADT.Shields;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;

namespace Content.Server.ADT.Shields;

public sealed partial class ShieldConduitSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly ShieldGeneratorSystem _generator = default!;

    private static readonly Direction[] CardinalDirs =
    {
        Direction.North,
        Direction.South,
        Direction.East,
        Direction.West,
    };

    public override void Initialize()
    {
        SubscribeLocalEvent<ShieldConduitComponent, ComponentInit>(OnConduitInit);
        SubscribeLocalEvent<ShieldConduitComponent, AnchorStateChangedEvent>(OnConduitAnchorChanged);
        SubscribeLocalEvent<ShieldConduitComponent, ComponentShutdown>(OnConduitShutdown);
    }

    private void OnConduitInit(EntityUid uid, ShieldConduitComponent conduit, ComponentInit args)
        => UpdateConduitConnection(uid, conduit);

    private void OnConduitAnchorChanged(EntityUid uid, ShieldConduitComponent conduit, ref AnchorStateChangedEvent args)
        => UpdateConduitConnection(uid, conduit);

    private void OnConduitShutdown(EntityUid uid, ShieldConduitComponent conduit, ComponentShutdown args)
    {
        var genUid = conduit.Generator;
        conduit.Generator = null;

        if (genUid is not { } generator || !TryComp<ShieldGeneratorComponent>(generator, out var gen))
            return;

        _generator.ScanConduits(generator, gen);
    }

    public void UpdateConduitConnection(EntityUid uid, ShieldConduitComponent conduit)
    {
        var xform = Transform(uid);
        if (!xform.Anchored || !TryFindGeneratorOnRequiredSide(xform, out var genUid, out var genXform))
        {
            Disconnect(uid, conduit);
            return;
        }

        var alreadyLinked = conduit.Generator == genUid;
        conduit.Generator = genUid;

        var delta = genXform.Coordinates.Position - xform.Coordinates.Position;
        _transform.SetLocalRotation(xform, delta.ToWorldAngle());
        UpdateVisual(uid, true);

        if (!alreadyLinked && TryComp<ShieldGeneratorComponent>(genUid, out var gen))
            _generator.ScanConduits(genUid, gen);
    }

    private void Disconnect(EntityUid uid, ShieldConduitComponent conduit)
    {
        var prevGen = conduit.Generator;
        conduit.Generator = null;
        UpdateVisual(uid, false);

        if (prevGen is { } genUid && !TerminatingOrDeleted(genUid) && TryComp<ShieldGeneratorComponent>(genUid, out var gen))
            _generator.ScanConduits(genUid, gen);
    }

    private bool TryFindGeneratorOnRequiredSide(TransformComponent xform, out EntityUid genUid, out TransformComponent genXform)
    {
        genUid = EntityUid.Invalid;
        genXform = null!;

        if (xform.GridUid is not { } gridUid || !TryComp<MapGridComponent>(gridUid, out var grid))
            return false;

        var tile = _map.LocalToTile(gridUid, grid, xform.Coordinates);
        foreach (var dir in ShieldGeneratorSystem.RequiredDirections)
        {
            var enumerator = _map.GetAnchoredEntitiesEnumerator(gridUid, grid, tile + dir.ToIntVec());
            while (enumerator.MoveNext(out var ent))
            {
                if (TerminatingOrDeleted(ent.Value))
                    continue;

                if (HasComp<ShieldGeneratorComponent>(ent.Value))
                {
                    genUid = ent.Value;
                    genXform = Transform(ent.Value);
                    return true;
                }
            }
        }

        return false;
    }

    public void RefreshAdjacentConduits(EntityUid genUid, TransformComponent xform)
    {
        if (xform.GridUid is not { } gridUid || !TryComp<MapGridComponent>(gridUid, out var grid))
            return;

        var tile = _map.LocalToTile(gridUid, grid, xform.Coordinates);
        foreach (var dir in CardinalDirs)
        {
            var enumerator = _map.GetAnchoredEntitiesEnumerator(gridUid, grid, tile + dir.ToIntVec());
            while (enumerator.MoveNext(out var ent))
            {
                if (TryComp<ShieldConduitComponent>(ent.Value, out var conduit))
                    UpdateConduitConnection(ent.Value, conduit);
            }
        }
    }

    private void UpdateVisual(EntityUid uid, bool connected)
    {
        if (TryComp<AppearanceComponent>(uid, out _))
            _appearance.SetData(uid, ShieldConduitVisuals.Connected, connected);
    }
}