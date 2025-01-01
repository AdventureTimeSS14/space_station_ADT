using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Content.Shared.ADT.Salvage.Systems;

namespace Content.Shared.ADT.MiningShop;

public abstract class SharedMiningShopSystem : EntitySystem
{
    [Dependency] private readonly MiningPointsSystem _miningPoints = default!;
    [Dependency] private readonly INetManager _net = default!;


    public override void Initialize()
    {


        Subs.BuiEvents<MiningShopComponent>(MiningShopUI.Key, subs =>
        {
            subs.Event<MiningShopBuiMsg>(OnVendBui);
            subs.Event<MiningShopExpressDeliveryBuiMsg>(OnVendBuiExpress);
        });
    }
    protected virtual void OnVendBui(Entity<MiningShopComponent> vendor, ref MiningShopBuiMsg args)
    {
        var comp = vendor.Comp;
        var sections = comp.Sections.Count;
        var actor = args.Actor;
        if (args.Section < 0 || args.Section >= sections)
        {
            Log.Error($"{ToPrettyString(actor)} sent an invalid vend section: {args.Section}. Max: {sections}");
            return;
        }

        var section = comp.Sections[args.Section];
        var entries = section.Entries.Count;
        if (args.Entry < 0 || args.Entry >= entries)
        {
            Log.Error($"{ToPrettyString(actor)} sent an invalid vend entry: {args.Entry}. Max: {entries}");
            return;
        }

        var entry = section.Entries[args.Entry];

        if (entry.Price != null)
        {
            if (_miningPoints.TryFindIdCard(actor) is {} idCard && _miningPoints.RemovePoints(idCard, entry.Price.Value))
            {
                Dirty(vendor);
            }
        }

        if (_net.IsClient)
            return;

        vendor.Comp.OrderList.Add(entry);
    }



    protected virtual void OnVendBuiExpress(Entity<MiningShopComponent> vendor, ref MiningShopExpressDeliveryBuiMsg args)
    {
        // намеренно пустое, все действия на сервере
    }
}

/// <summary>
/// Raised on a shop vendor to get its current balance.
/// A currency component sets Balance to whatever it is.
/// </summary>
[ByRefEvent]
public record struct ShopVendorBalanceEvent(EntityUid User, uint Balance = 0);
