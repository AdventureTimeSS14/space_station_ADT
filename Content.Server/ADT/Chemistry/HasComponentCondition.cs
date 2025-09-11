using Content.Shared.EntityEffects;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Chemistry;

/// <summary>
/// Условие для проверки наличия определённых компонентов у сущности.
/// Используется в системе эффектов реагентов для ограничения действия на конкретные типы сущностей.
/// </summary>
[UsedImplicitly]
public sealed partial class HasComponentCondition : EntityEffectCondition
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
    [DataField]
    public bool Invert;
    public override bool Condition(EntityEffectBaseArgs args)
    {
        var hasComp = false;
        foreach (var component in Components)
        {
            hasComp = args.EntityManager.HasComponent(args.TargetEntity,
                args.EntityManager.ComponentFactory.GetRegistration(component).Type);

            if (hasComp)
                break;
        }

        return hasComp ^ Invert;
    }

    public override string GuidebookExplanation(IPrototypeManager prototype)
    {
        return Loc.GetString("reagent-effect-condition-guidebook-has-component",
            ("comp", Loc.GetString(GuidebookComponentName)),
            ("invert", Invert));
    }
}
