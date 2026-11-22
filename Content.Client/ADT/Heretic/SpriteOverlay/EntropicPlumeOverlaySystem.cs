//

using Content.Shared.ADT.Heretic.Components;

namespace Content.Client.ADT.Heretic.SpriteOverlay;

public sealed class EntropicPlumeOverlaySystem : SpriteOverlaySystem<EntropicPlumeAffectedComponent>
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<EntropicPlumeAffectedComponent, AfterAutoHandleStateEvent>((uid, comp, _) =>
            AddOverlay(uid, comp));
    }
}
