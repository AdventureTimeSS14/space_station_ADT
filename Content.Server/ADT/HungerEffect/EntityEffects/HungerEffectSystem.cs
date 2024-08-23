using Robust.Shared.Audio.Systems;
using Content.Shared.Humanoid;
using Content.Shared.StatusEffect;
using Robust.Shared.Timing;
using Content.Shared.Database;
using Content.Shared.ADT.Hallucinations;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Content.Server.Mind;
using Content.Server.Body.Systems;
using Content.Shared.Nutrition.Components;
using Content.Server.Power.Components;

namespace Content.Server.HungerEffect;

public sealed partial class HungerEffectSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HungerEffectComponent, MapInitEvent>(OnHungerInit);
        SubscribeLocalEvent<HungerEffectComponent, ComponentShutdown>(OnHungerShutdown);
    }

    private void OnHungerInit(EntityUid uid, HungerEffectComponent component, MapInitEvent args)
    {
        if (TryComp<HungerComponent>(uid, out var hunger))
        {
            hunger.ActualDecayRate *= 300f;
            hunger.BaseDecayRate *= 300f;
        }
        if (TryComp<ThirstComponent>(uid, out var thirst))
        {
            thirst.ActualDecayRate *= 300f;
            thirst.BaseDecayRate *= 300f;
        }
    }

    private void OnHungerShutdown(EntityUid uid, HungerEffectComponent component, ComponentShutdown args)
    {
        if (TryComp<HungerComponent>(uid, out var hunger))
        {
            hunger.ActualDecayRate /= 300f;
            hunger.BaseDecayRate /= 300f;
        }
        if (TryComp<ThirstComponent>(uid, out var thirst))
        {
            thirst.ActualDecayRate /= 300f;
            thirst.BaseDecayRate /= 300f;
        }
    }
}
