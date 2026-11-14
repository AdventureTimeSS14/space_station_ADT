using Robust.Shared.Map;
using Robust.Shared.Network;

namespace Content.Shared.ADT.ChildEntities;

public sealed partial class ChildEntitiesSystem : EntitySystem
{
    [Dependency] private INetManager _net = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ChildEntitiesComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ChildEntitiesComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnMapInit(Entity<ChildEntitiesComponent> ent, ref MapInitEvent args)
    {
        if (!_net.IsServer)
            return;

        foreach (var child in ent.Comp.ChildPrototypes)
        {
            var coords = new EntityCoordinates(ent, child.Offset);

            var childEnt = PredictedSpawnAttachedTo(child.Prototype, coords);
            ent.Comp.Children.Add(childEnt);
        }

        Dirty(ent);
    }

    private void OnShutdown(Entity<ChildEntitiesComponent> ent, ref ComponentShutdown args)
    {
        if (!_net.IsServer)
            return;

        foreach (var child in ent.Comp.Children)
        {
            if (TerminatingOrDeleted(child))
                continue;

            PredictedQueueDel(child);
        }
    }
}