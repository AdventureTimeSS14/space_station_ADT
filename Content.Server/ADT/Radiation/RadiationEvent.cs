using Content.Shared.ADT.Radiation;
using Content.Shared.Radiation.Components;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Radiation;

public sealed partial class RadiationEvent : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    /// <summary>
    /// Система ставит указанное кол-во радиации. После чего, после n-ного времени, начинает уменьшать радиационый фон на x рады, за y секунд.
    /// </summary>
    public float TimeSinceStart = 0f;
    public float TimeSinceLastDecay = 0f;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<RadiationEventComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            comp.TimeSinceStart += frameTime;

            if (!TryComp<RadiationSourceComponent>(uid, out var radiation))
            {
                radiation = AddComp<RadiationSourceComponent>(uid);
                radiation.Intensity = comp.InitialIntensity;
                radiation.Slope = 0;
                radiation.Enabled = true;
                continue;
            }

            if (!comp.Decaying && !radiation.Enabled)
            {
                radiation.Intensity = comp.InitialIntensity;
                radiation.Slope = 0;
                radiation.Enabled = true;
            }

            if (!comp.Decaying && comp.TimeSinceStart >= comp.DecayDelay)
            {
                comp.Decaying = true;
                comp.TimeSinceLastDecay = 0f;
            }

            if (!comp.Decaying)
                continue;

            comp.TimeSinceLastDecay += frameTime;

            if (comp.TimeSinceLastDecay < comp.DecayRateInterval)
                continue;

            comp.TimeSinceLastDecay -= comp.DecayRateInterval;
            radiation.Intensity -= comp.DecayRate;

            if (radiation.Intensity <= 0f)
            {
                radiation.Intensity = 0f;
                radiation.Enabled = false;
                RemCompDeferred<RadiationEventComponent>(uid);
            }
        }
    }
}