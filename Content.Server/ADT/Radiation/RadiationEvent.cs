using Content.Server.Radiation.Systems;
using Content.Shared.ADT.Radiation;
using Content.Shared.Radiation.Components;
using Content.Shared.Radiation.Systems;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Radiation;

public sealed partial class RadiationEvent : EntitySystem
{
    [Dependency] private readonly RadiationSystem _radiation = default!;
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
                AddComp<RadiationSourceComponent>(uid);
                _radiation.SetIntensity(uid, comp.InitialIntensity);
                _radiation.SetSlope(uid, 0);
                continue;
            }

            if (!comp.Decaying && !radiation.Enabled)
            {
                _radiation.SetIntensity(uid, comp.InitialIntensity);
                _radiation.SetSlope(uid, 0);
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
            _radiation.SetIntensity(uid, Math.Max(0, radiation.Intensity - comp.DecayRate));

            if (radiation.Intensity <= 0f)
            {
                RemCompDeferred<RadiationEventComponent>(uid);
            }
        }
    }
}