using Content.Shared.Temperature.Components;

namespace Content.Shared.Temperature.Systems;

public abstract partial class SharedTemperatureSystem
{
    public void ForceChangeTemperature(EntityUid uid, float temp, TemperatureComponent? temperature = null)
    {
        if (!TemperatureQuery.Resolve(uid, ref temperature))
            return;

        var lastTemp = temperature.Temperature;
        var attemptEv = new TemperatureChangeAttemptEvent(temp, lastTemp, temp - lastTemp);
        RaiseLocalEvent(uid, attemptEv);
        if (attemptEv.Cancelled)
            return;

        SetTemperature((uid, temperature), temp);
    }

    private bool IsTemperatureChangeCancelled(Entity<TemperatureComponent> ent, float lastTemp)
    {
        var newTemp = ent.Comp.Temperature;
        if (MathHelper.CloseTo(newTemp, lastTemp))
            return false;

        var attemptEv = new TemperatureChangeAttemptEvent(newTemp, lastTemp, newTemp - lastTemp);
        RaiseLocalEvent(ent, attemptEv);
        if (!attemptEv.Cancelled)
            return false;

        ent.Comp.Temperature = lastTemp;
        return true;
    }
}
