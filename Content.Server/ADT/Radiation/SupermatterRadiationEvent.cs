using Content.Shared.ADT.Radiation;
using Content.Shared.Radiation.Components;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Radiation;

public sealed partial class SupermatterRadiationEvent : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    /// <summary>
    /// Система накладывает указанное кол-во радиации на ентити. После истечения 30 секунд, радиация начинает потихоньку угасать. 
    /// Важно уточнить, что пока система заточне исключительно для суперматерии.
    /// 
    /// TODO: Изменить систему для использования на сломанных РИТЭГах.
    /// </summary>
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<SupermatterRadiationEventComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            comp.TimeSinceStart += frameTime;

            if (!HasComp<RadiationSourceComponent>(uid))
            {
                var rad = EnsureComp<RadiationSourceComponent>(uid);
                rad.Intensity = comp.InitialIntensity;
                rad.Slope = 0;
                rad.Enabled = true;
                continue;
            }

            if (!comp.Decaying && comp.TimeSinceStart >= comp.DecayDelay)
            {
                comp.Decaying = true;
            }

            if (comp.Decaying && TryComp<RadiationSourceComponent>(uid, out var radiation))
            {
                radiation.Intensity -= comp.DecayRate * frameTime;

                if (radiation.Intensity <= 0f)
                {
                    radiation.Intensity = 0f;
                    radiation.Enabled = false;
                    RemCompDeferred<SupermatterRadiationEventComponent>(uid);
                }
            }
        }
    }
}