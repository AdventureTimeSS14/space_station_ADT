using Content.Shared.Temperature.Components;
using Robust.Shared.Physics.Components;

namespace Content.Server.Temperature.Systems;

public sealed partial class TemperatureSystem
{
    /// <summary>
    /// Guns added during map init can miss the mass-based heat capacity setup.
    /// Give them a capacity so a shot raises temperature instead of dividing by zero.
    /// </summary>
    public void EnsureGunHeatCapacity(Entity<TemperatureComponent> temperature, float heatPerShot)
    {
        if (temperature.Comp.HeatCapacity > 0f)
            return;

        var mass = CompOrNull<PhysicsComponent>(temperature)?.FixturesMass ?? 1f;
        temperature.Comp.HeatCapacity = Math.Max(mass * temperature.Comp.SpecificHeat, Math.Max(heatPerShot, 1f));
    }
}
