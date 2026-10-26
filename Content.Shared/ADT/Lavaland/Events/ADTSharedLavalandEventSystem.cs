using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Lavaland.Events;

public abstract class ADTSharedLavalandEventSystem : EntitySystem
{
    public abstract void RunEvent(EntityUid map, ProtoId<ADTLavalandEventPrototype> id);

    public abstract void ScatterSpawn(EntityUid map, EntProtoId prototype, int count, ADTScatterSpawnEffect effect);
}
