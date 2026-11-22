using Content.Server.ADT.Economy;
using Content.Server.Cargo.Components;
using Content.Server.Cargo.Systems;
using Content.Shared.ADT.CCVar;
using Content.Shared.Cargo.Components;
using Content.Shared.Cargo.Prototypes;
using Content.Shared.GameTicking;
using Content.Shared.Stacks;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Server.Cargo.Systems;

public sealed partial class CargoSystem
{
    /// <summary>
    /// Стоимость заказа каталога с учётом спроса на товар и корректировок ЦК.
    /// Только чтение: компонент рынка создаётся при фиксации покупки, НЕ ЗДЕСЬ.
    /// </summary>
    public int AdtOrderCost(EntityUid station, CargoProductPrototype product, int quantity)
    {
        var market = AdtDemandMarket(station);

        var cost = (double) AdtProductCost(product) * quantity;

        if (market != null)
            cost *= market.GetSellMultiplier(product.ID, Timing.CurTime);

        cost *= AdtCategoryMultiplier(station, product.Category);

        return (int) Math.Round(cost);
    }

    private DemandMarketComponent? AdtDemandMarket(EntityUid station)
    {
        if (!_cfg.GetCVar(ADTCCVars.MarketDeflation) ||
            !TryComp<StationBankAccountComponent>(station, out _) ||
            !TryComp<DemandMarketComponent>(station, out var market))
            return null;

        return market;
    }

    private double AdtCategoryMultiplier(EntityUid station, string category)
    {
        if (TryComp<StationCategorySurgeComponent>(station, out var surge) &&
            surge.Multipliers.TryGetValue(category, out var multiplier))
            return multiplier;

        return 1;
    }

    private StationMarketData? _adtMarket;

    private StationMarketData? AdtDeflationMarket()
    {
        if (!_cfg.GetCVar(ADTCCVars.MarketDeflation))
            return null;

        _adtMarket ??= new StationMarketData();
        return _adtMarket;
    }

    private void AdtInitializeMarket()
    {
        SubscribeLocalEvent<RoundRestartCleanupEvent>(AdtOnRoundRestart);
    }

    private void AdtOnRoundRestart(RoundRestartCleanupEvent ev)
    {
        _adtMarket = null;
    }

    /// <summary>
    /// фиксация покупки - спрос на товар растёт. Единственное место,
    /// где компонент рынка заказов создаётся.
    /// </summary>
    public void AdtRecordPurchase(EntityUid station, CargoProductPrototype product, int units)
    {
        if (!TryComp<StationBankAccountComponent>(station, out _))
            return;

        EnsureComp<DemandMarketComponent>(station).RecordTrade(product.ID, units, Timing.CurTime);
    }

    public Dictionary<ProtoId<CargoProductPrototype>, int> AdtProductCosts(EntityUid station, List<ProtoId<CargoProductPrototype>> products)
    {
        var costs = new Dictionary<ProtoId<CargoProductPrototype>, int>();

        var market = AdtDemandMarket(station);
        var now = Timing.CurTime;

        foreach (var id in products)
        {
            if (!ProtoMan.TryIndex(id, out var product))
                continue;

            var cost = (double) AdtProductCost(product);

            if (market != null)
                cost *= market.GetSellMultiplier(id, now);

            cost *= AdtCategoryMultiplier(station, product.Category);

            costs[id] = (int) Math.Round(cost);
        }

        return costs;
    }


    private void AdtAdjustMarketPrices(EntityUid gridUid, List<(EntityUid Ent, OverrideSellComponent? Sell, double Price)> pending)
    {
        if (!_cfg.GetCVar(ADTCCVars.MarketDeflation) || pending.Count == 0)
            return;

        var market = AdtDeflationMarket();
        if (market == null)
            return;

        var topLevel = new HashSet<EntityUid>();
        foreach (var (ent, _, _) in pending)
            topLevel.Add(ent);

        // размер партии по прототипам - чтобы скидка учитывала саму партию
        var batch = new Dictionary<string, int>();
        foreach (var (ent, _, _) in pending)
            AdtCountGoods(ent, batch, topLevel);

        var now = Timing.CurTime;
        var mults = new Dictionary<string, double>();
        foreach (var (protoId, units) in batch)
            mults[protoId] = market.GetBatchMultiplier(protoId, units, now);

        for (var i = 0; i < pending.Count; i++)
        {
            var (ent, sell, price) = pending[i];
            pending[i] = (ent, sell, AdtDeflatePrice(ent, price, mults, topLevel));
        }
    }

    private void AdtCountGoods(EntityUid ent, Dictionary<string, int> batch, HashSet<EntityUid> topLevel)
    {
        if (MetaData(ent).EntityPrototype?.ID is { } protoId)
        {
            var units = TryComp<StackComponent>(ent, out var stack) ? Math.Max(1, (int) stack.Count) : 1;
            batch[protoId] = batch.GetValueOrDefault(protoId) + units;
        }

        if (!TryComp<ContainerManagerComponent>(ent, out var containers))
            return;

        foreach (var container in containers.Containers.Values)
        {
            foreach (var child in container.ContainedEntities)
            {
                if (topLevel.Contains(child))
                    continue;

                AdtCountGoods(child, batch, topLevel);
            }
        }
    }

    private double AdtDeflatePrice(EntityUid ent, double basePrice, Dictionary<string, double> mults, HashSet<EntityUid> topLevel)
    {
        var (raw, deflated) = AdtDecomposePrice(ent, mults, topLevel);
        return raw > 0 ? basePrice * (deflated / raw) : basePrice;
    }

    private (double Raw, double Deflated) AdtDecomposePrice(EntityUid ent, Dictionary<string, double> mults, HashSet<EntityUid> topLevel)
    {
        var raw = _pricing.GetPrice(ent, includeContents: false);
        var deflated = MetaData(ent).EntityPrototype?.ID is { } protoId
            ? raw * mults.GetValueOrDefault(protoId, 1)
            : raw;

        if (!TryComp<ContainerManagerComponent>(ent, out var containers))
            return (raw, deflated);

        foreach (var container in containers.Containers.Values)
        {
            foreach (var child in container.ContainedEntities)
            {
                if (topLevel.Contains(child))
                    continue;

                var (childRaw, childDeflated) = AdtDecomposePrice(child, mults, topLevel);
                raw += childRaw;
                deflated += childDeflated;
            }
        }

        return (raw, deflated);
    }

    public void AdtRefreshOrderConsoles(EntityUid station)
    {
        var query = AllEntityQuery<CargoOrderConsoleComponent>();

        while (query.MoveNext(out var console, out _))
        {
            if (_station.GetOwningStation(console) != station)
                continue;

            UpdateOrderState(console, station);
        }
    }

    /// <summary>
    /// Учёт продажи рынками. Вызывается ПОСЛЕ расчёта цен партии -
    /// счётчики растут уже вслед за той партией, что была оценена.
    /// Единственное место, где компоненты рынков создаются.
    /// </summary>
    private void AdtRegisterSale(EntityUid gridUid, EntityUid station, HashSet<(EntityUid, OverrideSellComponent?, double)> goods)
    {
        if (!_cfg.GetCVar(ADTCCVars.MarketDeflation) || goods.Count == 0)
            return;

        var market = AdtDeflationMarket();
        if (market == null)
            return;

        var now = Timing.CurTime;

        var topLevel = new HashSet<EntityUid>();
        foreach (var (ent, _, _) in goods)
            topLevel.Add(ent);

        var sold = new Dictionary<string, int>();
        foreach (var (ent, _, _) in goods)
            AdtCountGoods(ent, sold, topLevel);

        foreach (var (protoId, units) in sold)
            market.AddSold(protoId, units, now);
    }
}
