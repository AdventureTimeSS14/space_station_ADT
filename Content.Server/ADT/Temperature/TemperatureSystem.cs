using Content.Server.Temperature.Components;

namespace Content.Server.Temperature.Systems;

public sealed partial class TemperatureSystem
{
    public void SetHeatProtection(Entity<TemperatureProtectionComponent> ent, float coefficient)
    {
        ent.Comp.HeatingCoefficient = coefficient;
        ent.Comp.CoolingCoefficient = coefficient;
        Dirty(ent.Owner, ent.Comp);
    }
}
