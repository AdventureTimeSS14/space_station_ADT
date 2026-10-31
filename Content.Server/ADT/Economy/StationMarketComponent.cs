using Robust.Shared.Serialization;

namespace Content.Server.ADT.Economy;

[RegisterComponent]
public sealed partial class StationMarketComponent : Component
{
    public Dictionary<string, double> SoldUnits = new();

    public TimeSpan LastDecayTime;

    /// <summary>
    /// Скидка приёма за каждую проданную единицу (0.05 - минус 5% за штуку).
    /// </summary>
    [DataField]
    public float UnitDiscount = 0.05f;

    /// <summary>
    /// Нижний потолок множителя приёма (0.35 - максимум минус 65%).
    /// </summary>
    [DataField]
    public float MinMultiplier = 0.35f;

    [DataField]
    public TimeSpan HalfLife = TimeSpan.FromMinutes(20);

    public void AddSold(string protoId, int units, TimeSpan now)
    {
        Decay(now);
        SoldUnits[protoId] = SoldUnits.GetValueOrDefault(protoId) + units;
    }

    /// <summary>
    /// Множитель приёма для партии из batchUnits единиц прототипа.
    /// Партия сама проезжает по нарастающей скидке: усреднённый множитель
    /// считается от середины диапазона счётчика (before + (batch+1)/2),
    /// поэтому крупная партия дешевеет уже в момент продажи, а не со следующей.
    /// </summary>
    public double GetBatchMultiplier(string? protoId, int batchUnits, TimeSpan now)
    {
        Decay(now);

        var before = protoId != null && SoldUnits.TryGetValue(protoId, out var units) ? units : 0;
        if (before <= 0 && batchUnits <= 0)
            return 1;

        var avg = before + (batchUnits + 1) / 2.0;
        return Math.Max(MinMultiplier, 1 - avg * UnitDiscount);
    }

    private void Decay(TimeSpan now)
    {
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
