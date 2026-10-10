using Content.Shared.Damage.Components;

namespace Content.Shared.Destructible.Thresholds.Triggers;

/// <summary>
/// Триггер, проверяющий наличие определённых компонентов у сущности.
/// </summary>
[DataDefinition]
public sealed partial class HasComponentTrigger : IThresholdTrigger
{
    [Dependency] private EntityManager _entityManager = default!;

    /// <summary>
    /// Набор имён компонентов для проверки.
    /// </summary>
    [DataField(required: true)]
    public HashSet<string> Components = new();

    /// <summary>
    /// Инвертирует результат проверки.
    /// </summary>
    [DataField]
    public bool Invert;

    public bool Reached(Entity<DamageableComponent> damageable, SharedDestructibleSystem system)
    {
        var hasComp = false;

        foreach (var component in Components)
        {
            var registration = _entityManager.ComponentFactory.GetRegistration(component);
            hasComp = _entityManager.HasComponent(damageable, registration.Type);

            if (hasComp)
                break;
        }

        return hasComp ^ Invert;
    }

    public int CompareTo(IThresholdTrigger? other)
    {
        if (other is not HasComponentTrigger trigger)
            return 0;

        var count = Components.Count.CompareTo(trigger.Components.Count);
        if (count != 0)
            return count;

        return Invert.CompareTo(trigger.Invert);
    }

    public bool Equals(IThresholdTrigger? other)
    {
        return other is HasComponentTrigger trigger
               && Invert == trigger.Invert
               && Components.SetEquals(trigger.Components);
    }
}
