using Content.Shared.ADT.MiningShop;
using Robust.Server.GameObjects;
using Content.Server.Spawners.Components;
using Content.Server.Spawners.EntitySystems;
using Robust.Shared.Prototypes;
using System.Linq;
using Content.Shared.ADT.Droppods.EntitySystems;

namespace Content.Server.ADT.MiningShop;

public sealed class MiningShopSystem : SharedMiningShopSystem
{
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly DroppodSystem _droppod = default!;

    protected override void OnVendBui(Entity<MiningShopComponent> vendor, ref MiningShopBuiMsg args)
    {
        base.OnVendBui(vendor, ref args);

        var msg = new MiningShopRefreshBuiMsg();
        _ui.ServerSendUiMessage(vendor.Owner, args.UiKey, msg, args.Actor);
    }

    protected override void OnVendBuiExpress(Entity<MiningShopComponent> vendor, ref MiningShopExpressDeliveryBuiMsg args)
    {
        base.OnVendBuiExpress(vendor, ref args);

        var msg = new MiningShopExpressDeliveryBuiMsg();
        _ui.ServerSendUiMessage(vendor.Owner, args.UiKey, msg, args.Actor);

        var actor = args.Actor;
        if (vendor.Comp.OrderList.Count <= 0)
            return;
        if (!TryComp(actor, out TransformComponent? xform))
            return;

        List<EntProtoId> ids = vendor.Comp.OrderList.Select(entry => entry.Id).ToList();

        _droppod.CreateDroppod(xform.Coordinates, ids);
        vendor.Comp.OrderList.Clear();
    }
}
