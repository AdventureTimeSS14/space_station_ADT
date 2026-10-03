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
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.ADT.Nutrition;
using Content.Server.Power.Components;

namespace Content.Server.HungerEffect;

public sealed partial class HungerEffectSystem : EntitySystem
{
    [Dependency] private SatiationSystem _satiation = default!;

    private const float RateMultiplier = 300f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HungerEffectComponent, MapInitEvent>(OnHungerInit);
        SubscribeLocalEvent<HungerEffectComponent, ComponentShutdown>(OnHungerShutdown);
        SubscribeLocalEvent<HungerEffectComponent, ADTSatiationRateModifyEvent>(OnRateModify);
    }

    private void OnHungerInit(EntityUid uid, HungerEffectComponent component, MapInitEvent args)
    {
        RefreshRates(uid);
    }

    private void OnHungerShutdown(EntityUid uid, HungerEffectComponent component, ComponentShutdown args)
    {
        RefreshRates(uid);
    }

    private void OnRateModify(Entity<HungerEffectComponent> ent, ref ADTSatiationRateModifyEvent args)
    {
        if (ent.Comp.LifeStage >= ComponentLifeStage.Stopping)
            return;

        if (args.Type == SatiationSystem.Hunger || args.Type == SatiationSystem.Thirst)
            args.Multiplier *= RateMultiplier;
    }

    private void RefreshRates(EntityUid uid)
    {
        if (!TryComp<SatiationComponent>(uid, out var satiation))
            return;

        foreach (var type in new[] { SatiationSystem.Hunger, SatiationSystem.Thirst })
        {
            if (_satiation.GetValueOrNull((uid, satiation), type) is { } value)
                _satiation.SetValue((uid, satiation), type, value);
        }
    }
}
