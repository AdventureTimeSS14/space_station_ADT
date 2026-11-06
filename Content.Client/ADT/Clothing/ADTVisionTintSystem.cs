using System.Numerics;
using Content.Client.ADT.Overlays.Shaders;
using Content.Client.Overlays;
using Content.Shared.ADT.Clothing;
using Content.Shared.Inventory.Events;
using Content.Shared.Item.ItemToggle.Components;
using Robust.Client.Graphics;

namespace Content.Client.ADT.Clothing;

public sealed class ADTVisionTintSystem : EquipmentHudSystem<ADTVisionTintComponent>
{
    [Dependency] private readonly IOverlayManager _overlayMan = default!;

    private ADTVisionTintOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        _overlay = new();

        SubscribeLocalEvent<ADTVisionTintComponent, ItemToggledEvent>(OnToggled);
    }

    private void OnToggled(Entity<ADTVisionTintComponent> ent, ref ItemToggledEvent args)
    {
        RefreshOverlay();
    }

    protected override void OnRefreshComponentHud(Entity<ADTVisionTintComponent> ent, ref RefreshEquipmentHudEvent<ADTVisionTintComponent> args)
    {
        if (TryComp<ItemToggleComponent>(ent, out var toggle) && !toggle.Activated)
            return;

        base.OnRefreshComponentHud(ent, ref args);
    }

    protected override void UpdateInternal(RefreshEquipmentHudEvent<ADTVisionTintComponent> args)
    {
        base.UpdateInternal(args);

        if (args.Components.Count == 0)
            return;

        var comp = args.Components[0];

        _overlay.TintColor = new Vector3(comp.Color.R, comp.Color.G, comp.Color.B);
        _overlay.TintAmount = comp.Amount;

        _overlayMan.AddOverlay(_overlay);
    }

    protected override void DeactivateInternal()
    {
        base.DeactivateInternal();

        _overlayMan.RemoveOverlay(_overlay);
    }
}
