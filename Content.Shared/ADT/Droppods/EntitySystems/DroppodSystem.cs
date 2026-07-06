using Content.Shared.ADT.Droppods.Components;
using Robust.Shared.Map;
using Robust.Shared.Spawners;
using Robust.Shared.Prototypes;
namespace Content.Shared.ADT.Droppods.EntitySystems;

public sealed class DroppodSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DroppodComponent, TimedDespawnEvent>(OnDespawn);
    }

    private void OnDespawn(EntityUid uid, DroppodComponent comp, ref TimedDespawnEvent args)
    {
        if (!TryComp(uid, out TransformComponent? xform))
            return;

        if (comp.Prototypes != null)
        {
            foreach (var spawned in comp.Prototypes)
            {
                Spawn(spawned.Id, xform.Coordinates);
            }
        }
    }

    public void CreateDroppod(EntityCoordinates coords, List<EntProtoId> spawns)
    {
        var droppod = Spawn("ADTDroppodDropping", coords);
        if (!TryComp<DroppodComponent>(droppod, out var pod))
            return;
        foreach (var proto in spawns)
        {
            pod.Prototypes.Add(proto);
        }
    }
}
