//

using Content.Shared.ADT.Heretic.Components;
using Robust.Client.GameObjects;

namespace Content.Client.ADT.Heretic.SpriteOverlay;

public sealed class VoidCurseOverlaySystem : SpriteOverlaySystem<VoidCurseComponent>
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<VoidCurseComponent, AfterAutoHandleStateEvent>((uid, comp, _) =>
            AddOverlay(uid, comp));
    }

    protected override void UpdateOverlayLayer(Entity<SpriteComponent> ent,
        VoidCurseComponent comp,
        int layer,
        EntityUid? source = null)
    {
        base.UpdateOverlayLayer(ent, comp, layer, source);
        var state = comp.Stacks >= comp.MaxLifetime ? comp.OverlayStateMax : comp.OverlayStateNormal;
        Sprite.LayerSetRsiState(ent.AsNullable(), layer, state);
    }
}
