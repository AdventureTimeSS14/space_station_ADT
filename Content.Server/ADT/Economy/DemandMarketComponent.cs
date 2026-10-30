using Robust.Shared.Serialization;

namespace Content.Server.ADT.Economy;


[Serializable]
public sealed class DemandMarketData
{
    public double DemandBonus;

    public double IdleFall;

    public TimeSpan LastTradeTime;

    public TimeSpan LastUpdateTime;
}

[RegisterComponent]
public sealed partial class DemandMarketComponent : Component
{
    public Dictionary<string, DemandMarketData> Products = new();

    [DataField]
    public float DemandStep = 0.10f;

    [DataField]
    public float MaxMultiplier = 1.6f;

    [DataField]
    public float MinMultiplier = 0.6f;

    [DataField]
    public TimeSpan DemandHalfLife = TimeSpan.FromMinutes(30);

    [DataField]
    public TimeSpan FallDelay = TimeSpan.FromMinutes(10);

    [DataField]
    public float FallRatePerMinute = 0.01f;

    [DataField]
    public TimeSpan RecoveryHalfLife = TimeSpan.FromMinutes(3);

    public double GetSellMultiplier(string protoId, TimeSpan now)
    {
        var data = GetOrAdd(protoId, now);
        Advance(data, now);
        return Math.Clamp(1 + data.DemandBonus - data.IdleFall, MinMultiplier, MaxMultiplier);
    }

    public void RecordTrade(string protoId, int units, TimeSpan now)
    {
        var data = GetOrAdd(protoId, now);
        Advance(data, now);

        data.DemandBonus = Math.Min(data.DemandBonus + DemandStep * Math.Min(units, 3), MaxMultiplier - 1);
        data.LastTradeTime = now;
    }

    private DemandMarketData GetOrAdd(string protoId, TimeSpan now)
    {
        if (Products.TryGetValue(protoId, out var data))
            return data;

        data = new DemandMarketData
        {
            LastTradeTime = now,
            LastUpdateTime = now,
        };
        Products[protoId] = data;
        return data;
    }

    private void Advance(DemandMarketData data, TimeSpan now)
    {
        var dt = now - data.LastUpdateTime;
        data.LastUpdateTime = now;

        if (dt <= TimeSpan.Zero)
            return;

        var minutes = dt.TotalMinutes;

        data.DemandBonus *= Math.Pow(0.5, dt.TotalSeconds / DemandHalfLife.TotalSeconds);

        if (now - data.LastTradeTime > FallDelay)
        {
            data.IdleFall = Math.Min(data.IdleFall + FallRatePerMinute * minutes, 1 - MinMultiplier);
        }
        else
        {
            data.IdleFall *= Math.Pow(0.5, dt.TotalSeconds / RecoveryHalfLife.TotalSeconds);
        }
    }
}
