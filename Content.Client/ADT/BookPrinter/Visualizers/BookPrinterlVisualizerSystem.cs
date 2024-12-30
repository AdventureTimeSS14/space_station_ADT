// Оригинал данного файла был сделан @temporaldarkness (discord). Код был взят с https://github.com/ss14-ganimed/ENT14-Master.

using Content.Shared.ADT.BookPrinter.Components;
using Content.Shared.Containers.ItemSlots;
using Robust.Client.GameObjects;

namespace Content.Client.ADT.BookPrinter.Visualizers;

public sealed partial class BookPrinterVisualizerSystem : VisualizerSystem<BookPrinterVisualsComponent>
{
    [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;

    protected override void OnAppearanceChange(EntityUid uid, BookPrinterVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null || !EntityManager.TryGetComponent<ItemSlotsComponent>(uid, out var slotComp))
            return;

        if (args.Sprite.LayerMapTryGet(BookPrinterVisualLayers.Working, out var workLayer))
        {
            args.Sprite.LayerSetVisible(workLayer, component.DoWorkAnimation);
        }

        if (args.Sprite.LayerMapTryGet(BookPrinterVisualLayers.Slotted, out var slotLayer))
        {
            args.Sprite.LayerSetVisible(slotLayer, (_itemSlotsSystem.GetItemOrNull(uid, "cartridgeSlot") is not null));
        }

        var cartridge = _itemSlotsSystem.GetItemOrNull(uid, "cartridgeSlot");

        if (args.Sprite.LayerMapTryGet(BookPrinterVisualLayers.Full, out var fullLayer))
        {
            args.Sprite.LayerSetVisible(fullLayer, false);
            if (cartridge is not null && EntityManager.TryGetComponent<BookPrinterCartridgeComponent>(cartridge, out BookPrinterCartridgeComponent? cartridgeComp))
                args.Sprite.LayerSetVisible(fullLayer!, cartridgeComp.CurrentCharge == cartridgeComp.FullCharge);
        }

        if (args.Sprite.LayerMapTryGet(BookPrinterVisualLayers.High, out var highLayer))
        {
            args.Sprite.LayerSetVisible(highLayer, false);
            if (cartridge is not null && EntityManager.TryGetComponent<BookPrinterCartridgeComponent>(cartridge, out BookPrinterCartridgeComponent? cartridgeComp))
                args.Sprite.LayerSetVisible(highLayer, cartridgeComp.CurrentCharge >= cartridgeComp.FullCharge / 1.43f && cartridgeComp.CurrentCharge < cartridgeComp.FullCharge);
        }

        if (args.Sprite.LayerMapTryGet(BookPrinterVisualLayers.Medium, out var mediumLayer))
        {
            args.Sprite.LayerSetVisible(mediumLayer, false);
            if (cartridge is not null && EntityManager.TryGetComponent<BookPrinterCartridgeComponent>(cartridge, out BookPrinterCartridgeComponent? cartridgeComp))
                args.Sprite.LayerSetVisible(mediumLayer, cartridgeComp.CurrentCharge >= cartridgeComp.FullCharge / 2.5f && cartridgeComp.CurrentCharge < cartridgeComp.FullCharge / 1.43f);
        }

        if (args.Sprite.LayerMapTryGet(BookPrinterVisualLayers.Low, out var lowLayer))
        {
            args.Sprite.LayerSetVisible(lowLayer, false);
            if (cartridge is not null && EntityManager.TryGetComponent<BookPrinterCartridgeComponent>(cartridge, out BookPrinterCartridgeComponent? cartridgeComp))
                args.Sprite.LayerSetVisible(lowLayer, cartridgeComp.CurrentCharge > 0 && cartridgeComp.CurrentCharge < cartridgeComp.FullCharge / 2.5f);
        }

        if (args.Sprite.LayerMapTryGet(BookPrinterVisualLayers.None, out var noneLayer))
        {
            args.Sprite.LayerSetVisible(noneLayer, false);
            if (cartridge is not null && EntityManager.TryGetComponent<BookPrinterCartridgeComponent>(cartridge, out BookPrinterCartridgeComponent? cartridgeComp))
                args.Sprite.LayerSetVisible(noneLayer, cartridgeComp.CurrentCharge < 1);
        }
    }
}
