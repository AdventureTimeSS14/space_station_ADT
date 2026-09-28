//

using Content.Shared.Temperature.Components;
using Content.Server.Temperature.Systems;
using Content.Shared.ADT.Heretic.Components;
using Content.Shared.ADT.Heretic.Systems;
using Content.Shared.Atmos;
using Content.Shared.Speech.Muting;
using Content.Shared.StatusEffect;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Heretic.EntitySystems.PathSpecific;

public sealed class VoidCurseSystem : SharedVoidCurseSystem
{
    [Dependency] private readonly TemperatureSystem _temp = default!;
    [Dependency] private readonly Content.Shared.StatusEffectNew.StatusEffectsSystem _statusEffect = default!;

    private static readonly EntProtoId MuteEffect = "StatusEffectMuted";

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var eqe = EntityQueryEnumerator<VoidCurseComponent>();
        while (eqe.MoveNext(out var uid, out var comp))
        {
            if (comp.Lifetime <= 0)
            {
                if (comp.Stacks <= 1)
                    RemCompDeferred(uid, comp);
                else
                {
                    comp.Stacks -= 1;
                    RefreshLifetime(comp);
                    Dirty(uid, comp);
                }
                continue;
            }

            comp.Timer -= frameTime;
            if (comp.Timer > 0)
                continue;

            comp.Timer = 1f;
            comp.Lifetime -= 1f;

            Cycle((uid, comp));
        }
    }

    private void Cycle(Entity<VoidCurseComponent> ent)
    {
        if (TryComp<TemperatureComponent>(ent, out var temp))
        {
            // temperaturesystem is not idiotproof :(
            var t = temp.CurrentTemperature - 3f * ent.Comp.Stacks;
            _temp.ForceChangeTemperature(ent, Math.Clamp(t, Atmospherics.TCMB, Atmospherics.Tmax), temp);
        }

        _statusEffect.TryUpdateStatusEffectDuration(ent, MuteEffect, TimeSpan.FromSeconds(5));
    }
}
