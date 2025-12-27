using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects;

public sealed partial class AddComponentsEffect : EntityEffect
{
    [DataField("components", required: true)]
    public ComponentRegistry Components = new();

    [DataField("replace")]
    public bool Replace = false;

    [DataField("scale")]
    public float Scale = 1.0f;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return Loc.GetString("reagent-effect-guidebook-add-component");
    }

    public override void Effect(EntityEffectBaseArgs args)
    {
        var entityManager = args.EntityManager;
        var target = args.TargetEntity;

        foreach (var (compName, registration) in Components)
        {
            var componentType = registration.Component.GetType();
            if (entityManager.HasComponent(target, componentType))
            {
                if (!Replace)
                    continue;

                entityManager.RemoveComponent(target, componentType);
            }

            var component = (Component)registration.Component;
            entityManager.AddComponent(target, component, true);
        }
    }
}
