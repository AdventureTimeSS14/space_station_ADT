namespace Content.Server.ADT.Economy;

public sealed class StationMarketData
{
    public Dictionary<string, double> SoldUnits = new();

    public TimeSpan LastDecayTime;

    /// <summary>
    /// Скидка приёма за каждую проданную единицу (0.05 - минус 5% за штуку).
    /// </summary>
    public float UnitDiscount = 0.05f;

    /// <summary>
    /// Нижний потолок множителя приёма (0.35 - максимум минус 65%).
    /// </summary>
    public float MinMultiplier = 0.35f;

    public TimeSpan HalfLife = TimeSpan.FromMinutes(20);

    public void AddSold(string protoId, int units, TimeSpan now)
    {
        Decay(now);
        SoldUnits[protoId] = SoldUnits.GetValueOrDefault(protoId) + units;
    }

    public double GetBatchMultiplier(string? protoId, int batchUnits, TimeSpan now)
    {
        Decay(now);

        if (batchUnits <= 0)
            return 1;

        var discount = (double) UnitDiscount;
        if (discount <= 0)
            return 1;

        var before = protoId != null && SoldUnits.TryGetValue(protoId, out var units) ? units : 0;
        var min = (double) MinMultiplier;

        var unitsUntilMin = (1.0 - min) / discount - before;
        var progressiveUnits = Math.Clamp((int) Math.Floor(unitsUntilMin + 1e-9), 0, batchUnits);
        var minUnits = batchUnits - progressiveUnits;

        var sum = progressiveUnits
            - discount * (before * progressiveUnits + (double) progressiveUnits * (progressiveUnits + 1) / 2.0)
            + minUnits * min;

        return Math.Max(min, sum / batchUnits);
    }

    private void Decay(TimeSpan now)
    {
        if (now == LastDecayTime)
            return;

        var dt = now - LastDecayTime;
        LastDecayTime = now;

        if (SoldUnits.Count == 0 || dt <= TimeSpan.Zero)
            return;

        var factor = Math.Pow(0.5, dt.TotalSeconds / HalfLife.TotalSeconds);
        List<string> faded = new();

        foreach (var (protoId, units) in SoldUnits)
        {
            var decayed = units * factor;

            if (decayed < 0.01)
                faded.Add(protoId);
            else
                SoldUnits[protoId] = decayed;
        }

        foreach (var protoId in faded)
            SoldUnits.Remove(protoId);
    }
}
