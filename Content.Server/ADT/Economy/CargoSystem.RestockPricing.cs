using Content.Shared.ADT.VendingMachines;
using Content.Shared.Cargo.Prototypes;
using Content.Shared.Containers;
using Content.Shared.EntityTable.EntitySelectors;
using Content.Shared.EntityTable.ValueSelector;
using Content.Shared.VendingMachines.Components;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;

namespace Content.Server.Cargo.Systems;

public sealed partial class CargoSystem
{
    private const int AdtFallbackItemPrice = 25;

    private const int AdtMaxTableDepth = 8;

    private readonly Dictionary<ProtoId<CargoProductPrototype>, int> AdtRestockCosts = new();

    private readonly Dictionary<ProtoId<VendingMachineInventoryPrototype>, int> AdtRestockPacks = new();

    private ISawmill _restockSawmill = default!;

    [Dependency] private ILogManager _logManager = default!;

    public int AdtProductCost(CargoProductPrototype product)
    {
        return AdtRestockCosts.TryGetValue(product.ID, out var cost) ? cost : product.Cost;
    }

    private void AdtInitializeRestockPricing()
    {
        _restockSawmill = _logManager.GetSawmill("cargo.restock");

        foreach (var product in ProtoMan.EnumeratePrototypes<CargoProductPrototype>())
        {
            if (product.Abstract || !ProtoMan.TryIndex(product.Product, out var entityProto))
                continue;

            AdtRestockPacks.Clear();
            AdtCollectRestockPacks(entityProto, product.ID, AdtRestockPacks, 0);

            if (AdtRestockPacks.Count == 0)
                continue;

            var total = 0;
            foreach (var (pack, count) in AdtRestockPacks)
                total += AdtGetPackPrice(pack) * count;

            if (total <= 0)
                continue;

            if (total != product.Cost)
                _restockSawmill.Info($"{product.ID}: cost {product.Cost} -> {total} (packs: {string.Join(", ", AdtRestockPacks)})");

            AdtRestockCosts[product.ID] = total;
        }
    }

    private void AdtCollectRestockPacks(EntityPrototype proto, string context, Dictionary<ProtoId<VendingMachineInventoryPrototype>, int> packs, int depth)
    {
        if (depth > AdtMaxTableDepth)
        {
            _restockSawmill.Warning($"Max table depth exceeded while pricing {context} (proto: {proto.ID})");
            return;
        }

        if (proto.Components.TryGetValue(Factory.GetComponentName<VendingMachineRestockComponent>(), out var restock) &&
            restock.Component is VendingMachineRestockComponent vRestock)
        {
            foreach (var pack in vRestock.CanRestock)
                packs[pack] = packs.GetValueOrDefault(pack) + 1;

            return;
        }

        if (proto.Components.TryGetValue(Factory.GetComponentName<EntityTableContainerFillComponent>(), out var fill) &&
            fill.Component is EntityTableContainerFillComponent fillComp)
        {
            foreach (var table in fillComp.Containers.Values)
                AdtCollectRestockPacks(table, context, packs, depth + 1);
        }
    }

    private void AdtCollectRestockPacks(EntityTableSelector selector, string context, Dictionary<ProtoId<VendingMachineInventoryPrototype>, int> packs, int depth)
    {
        if (depth > AdtMaxTableDepth)
        {
            _restockSawmill.Warning($"Max table depth exceeded while pricing {context}");
            return;
        }

        switch (selector)
        {
            case EntSelector ent:
            {
                if (!ProtoMan.TryIndex(ent.Id, out var entityProto))
                    break;

                var amount = AdtGetStaticAmount(ent.Amount);
                var local = new Dictionary<ProtoId<VendingMachineInventoryPrototype>, int>();
                AdtCollectRestockPacks(entityProto, context, local, depth + 1);

                foreach (var (pack, count) in local)
                    packs[pack] = packs.GetValueOrDefault(pack) + count * amount;

                break;
            }
            case AllSelector all:
                foreach (var child in all.Children)
                    AdtCollectRestockPacks(child, context, packs, depth + 1);
                break;
            case GroupSelector group:
                foreach (var child in group.Children)
                    AdtCollectRestockPacks(child, context, packs, depth + 1);
                break;
            case NestedSelector nested:
                if (ProtoMan.TryIndex(nested.TableId, out var tableProto))
                    AdtCollectRestockPacks(tableProto.Table, context, packs, depth + 1);
                break;
            case NoneSelector:
                break;
            default:
                _restockSawmill.Warning($"Unsupported entity table selector {selector.GetType().Name} while pricing {context}");
                break;
        }
    }

    private int AdtGetStaticAmount(NumberSelector amount) => amount switch
    {
        ConstantNumberSelector constant => Math.Max(1, constant.Value),
        RangeNumberSelector range => Math.Max(1, (range.Range.X + range.Range.Y) / 2),
        BinomialNumberSelector binomial => Math.Max(1, (int) Math.Round(binomial.Trials * binomial.Chance)),
        _ => 1,
    };

    private int AdtGetPackPrice(ProtoId<VendingMachineInventoryPrototype> packId)
    {
        if (!ProtoMan.TryIndex(packId, out var pack))
            return 0;

        var total = 0;
        foreach (var (id, amount, price, _) in VendingMachineInventoryData.Flatten(pack.StartingInventory))
        {
            var unit = price;
            if (unit <= 0 && ProtoMan.TryIndex(id, out var itemProto))
                unit = AdtGetEntryPrice(itemProto);

            total += Math.Max(unit, 0) * (int) amount;
        }

        return total;
    }

    private int AdtGetEntryPrice(EntityPrototype proto)
    {
        var price = (int) _pricing.GetEstimatedPrice(proto);
        return price > 0 ? price : AdtFallbackItemPrice;
    }
}
