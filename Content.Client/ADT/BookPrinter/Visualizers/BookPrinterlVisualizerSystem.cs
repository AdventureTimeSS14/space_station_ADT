// Оригинал данного файла был сделан @temporaldarkness (discord). Код был взят с https://github.com/ss14-ganimed/ENT14-Master.

using Content.Shared.ADT.BookPrinter.Components;
using Content.Shared.Containers.ItemSlots;
using Robust.Client.GameObjects;

namespace Content.Client.ADT.BookPrinter.Visualizers;

public sealed partial class BookPrinterVisualizerSystem : VisualizerSystem<BookPrinterVisualsComponent>
{
    [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;
    [Dependency] private readonly SpriteSystem _spriteSystem = default!;

    protected override void OnAppearanceChange(EntityUid uid, BookPrinterVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null || !TryComp<ItemSlotsComponent>(uid, out var slotComp))
            return;

        if (_spriteSystem.LayerMapTryGet((uid, args.Sprite), BookPrinterVisualLayers.Working, out var workLayer, false))
        {
            _spriteSystem.LayerSetVisible((uid, args.Sprite), workLayer, component.DoWorkAnimation);
        }

        if (_spriteSystem.LayerMapTryGet((uid, args.Sprite), BookPrinterVisualLayers.Slotted, out var slotLayer, false))
        {
            _spriteSystem.LayerSetVisible((uid, args.Sprite), slotLayer, (_itemSlotsSystem.GetItemOrNull(uid, "cartridgeSlot") is not null));
        }

        var cartridge = _itemSlotsSystem.GetItemOrNull(uid, "cartridgeSlot");

        if (_spriteSystem.LayerMapTryGet((uid, args.Sprite), BookPrinterVisualLayers.Full, out var fullLayer, false))
        {
            _spriteSystem.LayerSetVisible((uid, args.Sprite), fullLayer, false);
            if (cartridge is not null && TryComp<BookPrinterCartridgeComponent>(cartridge, out BookPrinterCartridgeComponent? cartridgeComp))
                _spriteSystem.LayerSetVisible((uid, args.Sprite), fullLayer, cartridgeComp.CurrentCharge == cartridgeComp.FullCharge);
        }

        if (_spriteSystem.LayerMapTryGet((uid, args.Sprite), BookPrinterVisualLayers.High, out var highLayer, false))
        {
            _spriteSystem.LayerSetVisible((uid, args.Sprite), highLayer, false);
            if (cartridge is not null && TryComp<BookPrinterCartridgeComponent>(cartridge, out BookPrinterCartridgeComponent? cartridgeComp))
                _spriteSystem.LayerSetVisible((uid, args.Sprite), highLayer, cartridgeComp.CurrentCharge >= cartridgeComp.FullCharge / 1.43f && cartridgeComp.CurrentCharge < cartridgeComp.FullCharge);
        }

        if (_spriteSystem.LayerMapTryGet((uid, args.Sprite), BookPrinterVisualLayers.Medium, out var mediumLayer, false))
        {
            _spriteSystem.LayerSetVisible((uid, args.Sprite), mediumLayer, false);
            if (cartridge is not null && TryComp<BookPrinterCartridgeComponent>(cartridge, out BookPrinterCartridgeComponent? cartridgeComp))
                _spriteSystem.LayerSetVisible((uid, args.Sprite), mediumLayer, cartridgeComp.CurrentCharge >= cartridgeComp.FullCharge / 2.5f && cartridgeComp.CurrentCharge < cartridgeComp.FullCharge / 1.43f);
        }

        if (_spriteSystem.LayerMapTryGet((uid, args.Sprite), BookPrinterVisualLayers.Low, out var lowLayer, false))
        {
            _spriteSystem.LayerSetVisible((uid, args.Sprite), lowLayer, false);
            if (cartridge is not null && TryComp<BookPrinterCartridgeComponent>(cartridge, out BookPrinterCartridgeComponent? cartridgeComp))
                _spriteSystem.LayerSetVisible((uid, args.Sprite), lowLayer, cartridgeComp.CurrentCharge > 0 && cartridgeComp.CurrentCharge < cartridgeComp.FullCharge / 2.5f);
        }

        if (_spriteSystem.LayerMapTryGet((uid, args.Sprite), BookPrinterVisualLayers.None, out var noneLayer, false))
        {
            _spriteSystem.LayerSetVisible((uid, args.Sprite), noneLayer, false);
            if (cartridge is not null && TryComp<BookPrinterCartridgeComponent>(cartridge, out BookPrinterCartridgeComponent? cartridgeComp))
                _spriteSystem.LayerSetVisible((uid, args.Sprite), noneLayer, cartridgeComp.CurrentCharge < 1);
        }
    }
}
