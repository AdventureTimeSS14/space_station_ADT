using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Content.Shared.ADT.Salvage.Systems;

namespace Content.Shared.ADT.MiningShop;

public abstract class SharedMiningShopSystem : EntitySystem
{
    [Dependency] private readonly MiningPointsSystem _miningPoints = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;


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
        if (_net.IsClient)
            return;

        if (!TryGetShopEntry(args.Entry, out var entry))
            return;

        if (entry.Price is { } price && price > 0)
        {
            if (_miningPoints.TryFindIdCard(args.Actor) is not { } idCard)
                return;

            if (!_miningPoints.RemovePoints(idCard, price))
                return;
        }

        vendor.Comp.OrderList.Add(entry);
        Dirty(vendor);
    }

    private bool TryGetShopEntry(MiningShopEntry requested, [NotNullWhen(true)] out MiningShopEntry? entry)
    {
        foreach (var section in _prototypes.EnumeratePrototypes<SharedMiningShopSectionPrototype>())
        {
            foreach (var candidate in section.Entries)
            {
                if (candidate.Id != requested.Id || candidate.Price != requested.Price || candidate.Name != requested.Name)
                    continue;

                entry = candidate;
                return true;
            }
        }

        entry = null;
        return false;
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
