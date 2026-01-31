using Content.Shared.ADT.SpaceWhale;
using Robust.Shared.Map;

namespace Content.Server.ADT.SpaceWhale;

public sealed class TailedEntitySystem : SharedTailedEntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TailedEntityComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<TailedEntityComponent, ComponentShutdown>(OnComponentShutdown);
    }

    private void OnMapInit(Entity<TailedEntityComponent> ent, ref MapInitEvent args)
    {
        InitializeTailSegments(ent);
    }

    private void OnComponentShutdown(Entity<TailedEntityComponent> ent, ref ComponentShutdown args)
    {
        foreach (var segment in ent.Comp.TailSegments)
            QueueDel(GetEntity(segment));

        ent.Comp.TailSegments.Clear();
    }

    private void InitializeTailSegments(Entity<TailedEntityComponent> ent, TransformComponent? xform = null)
    {
        if (!Resolve(ent.Owner, ref xform))
            return;

        var mapUid = xform.MapUid;
        if (mapUid == null)
            return;

        var headPos = _transformSystem.GetWorldPosition(xform);
        var headRot = _transformSystem.GetWorldRotation(xform);

        for (var i = 0; i < ent.Comp.Amount; i++)
        {
            var offset = headRot.ToWorldVec() * ent.Comp.Spacing * (i + 1);
            var spawnPos = headPos - offset;

            var segment = Spawn(ent.Comp.Prototype, new EntityCoordinates(mapUid.Value, spawnPos));
            ent.Comp.TailSegments.Add(GetNetEntity(segment));
        }

        Dirty(ent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<TailedEntityComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var comp, out var xform))
        {
            UpdateTailPositions((uid, comp, xform), frameTime);
        }
    }
}
