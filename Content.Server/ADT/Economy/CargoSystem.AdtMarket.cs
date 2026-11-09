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

    private bool AdtCategorySurged(EntityUid station, string category)
    {
        return TryComp<StationCategorySurgeComponent>(station, out var surge) &&
               surge.Multipliers.ContainsKey(category);
    }

    private StationMarketComponent? AdtDeflationMarket(EntityUid gridUid)
    {
        if (!_cfg.GetCVar(ADTCCVars.MarketDeflation))
            return null;

        var station = _station.GetOwningStation(gridUid);

        if (station != null && TryComp<StationBankAccountComponent>(station.Value, out _))
            return EnsureComp<StationMarketComponent>(station.Value);

        if (HasComp<TradeStationComponent>(gridUid))
            return EnsureComp<StationMarketComponent>(gridUid);

        return null;
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

            var cost = (double) product.Cost;

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

        var market = AdtDeflationMarket(gridUid);
        if (market == null)
            return;

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

            price *= market.GetBatchMultiplier(protoId, units, now);

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

        var market = AdtDeflationMarket(gridUid);
        if (market == null)
            return;

        var now = Timing.CurTime;

        foreach (var (ent, _, _) in goods)
        {
            if (MetaData(ent).EntityPrototype?.ID is not { } protoId)
                continue;

            var units = TryComp<StackComponent>(ent, out var stack) ? Math.Max(1, (int) stack.Count) : 1;
            market.AddSold(protoId, units, now);
        }
    }
}
