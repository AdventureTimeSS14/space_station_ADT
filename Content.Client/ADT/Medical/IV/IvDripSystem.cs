using Content.Shared.ADT.Medical.IV;
using Robust.Client.GameObjects;

namespace Content.Client.ADT.Medical.IV;

public sealed class IvDripSystem : SharedIvDripSystem
{
    // TODO: Добавить оверлей для капельницы, добавляет нитку от капельницы к пациенту. https://github.com/RMC-14/RMC-14
    // [Dependency] private readonly SpriteSystem _spriteSystem = default!;
    // [Dependency] private readonly IOverlayManager _overlay = default!;
    //
    // public override void Initialize()
    // {
    //     base.Initialize();
    //     if (!_overlay.HasOverlay<IVDripOverlay>())
    //         _overlay.AddOverlay(new IVDripOverlay());
    //
    //     SubscribeNetworkEvent<DialysisDetachedEvent>(OnDialysisDetachedEvent);
    // }

    protected override void UpdateIVAppearance(Entity<IVDripComponent> iv)
    {
        if (!TryComp(iv, out SpriteComponent? sprite))
            return;

        var hookedState = iv.Comp.AttachedTo is null
            ? iv.Comp.UnattachedState
            : iv.Comp.AttachedState;
        sprite.LayerSetState(IVDripVisualLayers.Base, hookedState);

        string? reagentState = null;
        for (var i = iv.Comp.ReagentStates.Count - 1; i >= 0; i--)
        {
            var (amount, state) = iv.Comp.ReagentStates[i];
            if (amount <= iv.Comp.FillPercentage)
            {
                reagentState = state;
                break;
            }
        }

        if (reagentState == null)
        {
            sprite.LayerSetVisible(IVDripVisualLayers.Reagent, false);
            return;
        }

        sprite.LayerSetVisible(IVDripVisualLayers.Reagent, true);
        sprite.LayerSetState(IVDripVisualLayers.Reagent, reagentState);
        sprite.LayerSetColor(IVDripVisualLayers.Reagent, iv.Comp.FillColor);
    }

    protected override void UpdatePackAppearance(Entity<BloodPackComponent> pack)
    {
        if (!TryComp(pack, out SpriteComponent? sprite))
            return;

        // TODO CM14 blood types
        if (sprite.LayerMapTryGet(BloodPackVisuals.Label, out var labelLayer))
            sprite.LayerSetVisible(labelLayer, false); // у наших пакетов нет состояние Laber
    }
}
