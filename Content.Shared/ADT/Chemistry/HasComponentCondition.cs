using Content.Shared.EntityEffects;
using JetBrains.Annotations;
using Content.Shared.EntityConditions;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.EffectConditions;

public sealed partial class HasComponentConditionSystem : EntityConditionSystem<MetaDataComponent, HasComponentCondition>
{
    protected override void Condition(Entity<MetaDataComponent> entity, ref EntityConditionEvent<HasComponentCondition> args)
    {
        var hasComp = false;
        foreach (var component in args.Condition.Components)
        {
            var reg = EntityManager.ComponentFactory.GetRegistration(component);
            hasComp = EntityManager.HasComponent(entity, reg.Type);

            if (hasComp)
                break;
        }

        args.Result = hasComp ^ args.Condition.Invert;
    }
}

/// <summary>
/// Условие для проверки наличия определённых компонентов у сущности.
/// Используется в системе эффектов реагентов для ограничения действия на конкретные типы сущностей.
/// </summary>
[UsedImplicitly]
public sealed partial class HasComponentCondition : EntityConditionBase<HasComponentCondition>
{
    /// <summary>
    /// Набор имён компонентов для проверки. Условие выполнится, если у сущности есть хотя бы один из них.
    /// </summary>
    [DataField(required: true)]
    public HashSet<string> Components = new();

    /// <summary>
    /// Локализованное название компонента для отображения в руководстве.
    /// </summary>
    [DataField(required: true)]
    public string GuidebookComponentName = default!;

    /// <summary>
    /// Инвертирует результат проверки условия.
    /// </summary>
    // Use base `Inverted` field from `EntityCondition`.
    [DataField]
    public bool Invert;

    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
    {
        return Loc.GetString("reagent-effect-condition-guidebook-has-component",
            ("comp", Loc.GetString(GuidebookComponentName)),
            ("invert", Inverted));
    }
}