using Content.Shared.ADT.SpaceWhale;
using Robust.Client.GameObjects;
using Robust.Shared.Map;

namespace Content.Client.ADT.SpaceWhale;

public sealed class TailedEntitySystem : SharedTailedEntitySystem
{
    private EntityQuery<SpriteComponent> _spriteQuery;

    public override void Initialize()
    {
        base.Initialize();
        _spriteQuery = GetEntityQuery<SpriteComponent>();
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        var query = EntityQueryEnumerator<TailedEntityComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var comp, out var xform))
        {
            UpdateTailPositions((uid, comp, xform), frameTime);
            UpdateTailLayers((uid, comp));
        }
    }

    private void UpdateTailLayers(Entity<TailedEntityComponent> ent)
    {
        if (_spriteQuery.TryGetComponent(ent.Owner, out var spriteSelf))
            spriteSelf.RenderOrder = (uint)ent.Comp.TailSegments.Count + 5;

        for (var i = 0; i < ent.Comp.TailSegments.Count; i++)
        {
            if (!TryGetEntity(ent.Comp.TailSegments[i], out var segment))
                continue;

            if (!_spriteQuery.TryGetComponent(segment, out var sprite))
                continue;

            sprite.RenderOrder = (uint)(ent.Comp.TailSegments.Count - i + 5);
        }
    }
}
