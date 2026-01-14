using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.EntityEffects.Effects;

public sealed partial class DelayedEntityEffect : EntityEffect
{
    [DataField(required: true)]
    public List<EntityEffect> DelayEffects = default!;

    [DataField(required: true)]
    public TimeSpan Delay;

    [DataField]
    public bool UseRandomDelay;

    public override void Effect(EntityEffectBaseArgs args)
    {
        var random = IoCManager.Resolve<IRobustRandom>();

        var actualDelay = Delay;
        if (UseRandomDelay)
        {
            var randomSeconds = random.NextDouble() * Delay.TotalSeconds;
            actualDelay = TimeSpan.FromSeconds(randomSeconds);
        }

        Timer.Spawn(actualDelay, () =>
        {
            if (!args.EntityManager.Deleted(args.TargetEntity))
            {
                foreach (var effect in DelayEffects)
                {
                    effect.Effect(args);
                }
            }
        });
    }

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        foreach (var effect in DelayEffects)
        {
            var effectText = effect.GuidebookEffectDescription(prototype, entSys);

            if (effectText == null)
                return null;

            var delayText = UseRandomDelay
                ? Loc.GetString("reagent-effect-guidebook-delayed-random",
                    ("maxdelay", Delay.TotalSeconds),
                    ("effect", effectText))
                : Loc.GetString("reagent-effect-guidebook-delayed",
                    ("delay", Delay.TotalSeconds),
                    ("effect", effectText));

            return delayText;
        }

        return null;
    }
}
