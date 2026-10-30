using Content.Server.ADT.Economy;
using Content.Server.Cargo.Components;
using Content.Server.Cargo.Systems;
using Content.Shared.ADT.CCVar;
using Content.Shared.Cargo.Components;
using Content.Shared.Cargo.Prototypes;
using Content.Shared.Stacks;
using Robust.Shared.Configuration;
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

        var cost = (double) product.Cost * quantity;

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

    /// <summary>
    /// фиксация покупки - спрос на товар растёт. Единственное место,
    /// где компонент рынка заказов создаётся.
    /// </summary>
    public void AdtRecordPurchase(EntityUid station, CargoProductPrototype product, int units)
    {
        if (!TryComp<StationBankAccountComponent>(station, out _) ||
            AdtCategoryMultiplier(station, product.Category) != 1)
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

            var cost = (double) product.Cost;

            if (market != null)
                cost *= market.GetSellMultiplier(id, now);

            cost *= AdtCategoryMultiplier(station, product.Category);

            costs[id] = (int) Math.Round(cost);
        }

        return costs;
    }


    /// <summary>
    /// Рыночные множители приёмки для собранной партии: станция - перегрев
    /// от перепроизводства (партия целиком проезжает по нарастающей скидке),
    /// торговая станция - спрос скупки.
    /// </summary>
    private void AdtAdjustMarketPrices(EntityUid gridUid, List<(EntityUid Ent, OverrideSellComponent? Sell, double Price)> pending)
    {
        if (!_cfg.GetCVar(ADTCCVars.MarketDeflation) || pending.Count == 0)
            return;

        var station = _station.GetOwningStation(gridUid);
        StationMarketComponent? stationMarket = null;
        DemandMarketComponent? demandMarket = null;

        var stationSale = station != null && TryComp<StationBankAccountComponent>(station.Value, out _);
        var tradeSale = !stationSale && TryComp<TradeStationComponent>(gridUid, out _);

        if (!stationSale && !tradeSale)
            return;

        if (stationSale)
            stationMarket = TryComp<StationMarketComponent>(station!.Value, out var sm) ? sm : new StationMarketComponent();
        else
            demandMarket = TryComp<DemandMarketComponent>(gridUid, out var dm) ? dm : new DemandMarketComponent();

        // размер партии по прототипам - чтобы скидка учитывала саму партию
        var batch = new Dictionary<string, int>();
        foreach (var (ent, _, _) in pending)
        {
            if (MetaData(ent).EntityPrototype?.ID is not { } protoId)
                continue;

            var units = TryComp<StackComponent>(ent, out var stack) ? Math.Max(1, (int) stack.Count) : 1;
            batch[protoId] = batch.GetValueOrDefault(protoId) + units;
        }

        var now = Timing.CurTime;
        for (var i = 0; i < pending.Count; i++)
        {
            if (MetaData(pending[i].Ent).EntityPrototype?.ID is not { } protoId)
                continue;

            var units = batch.GetValueOrDefault(protoId);
            var (ent, sell, price) = pending[i];

            if (stationSale)
                price *= stationMarket!.GetBatchMultiplier(protoId, units, now);
            else
                price *= demandMarket!.GetSellMultiplier(protoId, now);

            pending[i] = (ent, sell, price);
        }
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

        var now = Timing.CurTime;

        if (TryComp<StationBankAccountComponent>(station, out _))
        {
            var market = EnsureComp<StationMarketComponent>(station);

            foreach (var (ent, _, _) in goods)
            {
                if (MetaData(ent).EntityPrototype?.ID is not { } protoId)
                    continue;

                var units = TryComp<StackComponent>(ent, out var stack) ? Math.Max(1, (int) stack.Count) : 1;
                market.AddSold(protoId, units, now);
            }

            return;
        }

        if (!HasComp<TradeStationComponent>(gridUid))
            return;

        var tradeMarket = EnsureComp<DemandMarketComponent>(gridUid);

        foreach (var (ent, _, _) in goods)
        {
            if (MetaData(ent).EntityPrototype?.ID is not { } protoId)
                continue;

            var units = TryComp<StackComponent>(ent, out var stack) ? Math.Max(1, (int) stack.Count) : 1;
            tradeMarket.RecordTrade(protoId, units, now);
        }
    }
}
