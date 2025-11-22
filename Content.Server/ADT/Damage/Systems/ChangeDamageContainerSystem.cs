using Content.Shared.ADT.Damage.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using System.Linq;

namespace Content.Server.ADT.Damage.Systems;

public sealed class ChangeDamageContainerSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ChangeDamageContainerComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<ChangeDamageContainerComponent, ComponentRemove>(OnComponentRemove);
    }

    private void OnComponentStartup(EntityUid uid, ChangeDamageContainerComponent component, ComponentStartup args)
    {
        if (!TryComp<DamageableComponent>(uid, out var damageable))
            return;

        component.OriginalContainerId = damageable.DamageContainerID?.Id;
        ChangeDamageContainer(uid, component.ContainerId, damageable);
    }

    private void OnComponentRemove(EntityUid uid, ChangeDamageContainerComponent component, ComponentRemove args)
    {
        if (!TryComp<DamageableComponent>(uid, out var damageable) || component.OriginalContainerId == null)
            return;

        ChangeDamageContainer(uid, component.OriginalContainerId, damageable);
    }

    private void ChangeDamageContainer(EntityUid uid, string? containerId, DamageableComponent damageable)
    {
        // Сохраняем текущее состояние
        var oldDamage = damageable.Damage;
        var oldModifierSetId = damageable.DamageModifierSetId;
        var oldHealthBarThreshold = damageable.HealthBarThreshold;

        // Полностью удаляем компонент
        RemComp<DamageableComponent>(uid);

        // Создаем новый компонент через EntityManager
        var newComponent = EntityManager.AddComponent<DamageableComponent>(uid);

        // Используем рефлексию для установки DamageContainerID
        if (containerId != null)
        {
            var damageContainerIdProperty = typeof(DamageableComponent)
                .GetProperty("DamageContainerID",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

            if (damageContainerIdProperty != null && damageContainerIdProperty.CanWrite)
            {
                damageContainerIdProperty.SetValue(newComponent,
                    new ProtoId<DamageContainerPrototype>(containerId));
            }
        }

        // Используем публичные методы для установки остальных свойств
        if (oldModifierSetId != null)
        {
            _damageableSystem.SetDamageModifierSetId(uid, oldModifierSetId, newComponent);
        }

        // HealthBarThreshold - возможно, нужно установить через рефлексию, если нет публичного метода
        var healthBarThresholdProperty = typeof(DamageableComponent)
            .GetProperty("HealthBarThreshold",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        if (healthBarThresholdProperty != null && healthBarThresholdProperty.CanWrite)
        {
            healthBarThresholdProperty.SetValue(newComponent, oldHealthBarThreshold);
        }

        // Устанавливаем отфильтрованный урон
        var filteredDamage = FilterDamageByContainer(oldDamage, containerId);
        _damageableSystem.SetDamage(uid, newComponent, filteredDamage);

        // Помечаем компонент как измененный
        Dirty(uid, newComponent);
    }

    private DamageSpecifier FilterDamageByContainer(DamageSpecifier oldDamage, string? containerId)
    {
        var newDamage = new DamageSpecifier();

        if (containerId != null && _prototype.TryIndex<DamageContainerPrototype>(containerId, out var container))
        {
            // Получаем все поддерживаемые типы урона
            var supportedTypes = new HashSet<string>(container.SupportedTypes.Select(x => x.Id));

            // Добавляем типы из поддерживаемых групп
            foreach (var groupId in container.SupportedGroups)
            {
                if (_prototype.TryIndex<DamageGroupPrototype>(groupId, out var group))
                {
                    foreach (var type in group.DamageTypes.Select(x => x.Id))
                    {
                        supportedTypes.Add(type);
                    }
                }
            }

            // Переносим только поддерживаемые типы урона
            foreach (var (type, value) in oldDamage.DamageDict)
            {
                if (supportedTypes.Contains(type))
                {
                    newDamage.DamageDict[type] = value;
                }
            }

            // Добавляем нулевые значения для всех поддерживаемых типов
            foreach (var type in supportedTypes)
            {
                if (!newDamage.DamageDict.ContainsKey(type))
                {
                    newDamage.DamageDict[type] = FixedPoint2.Zero;
                }
            }
        }
        else
        {
            // Если контейнер не указан, используем все типы урона
            newDamage = new DamageSpecifier(oldDamage);

            // Добавляем все возможные типы урона
            foreach (var prototype in _prototype.EnumeratePrototypes<DamageTypePrototype>())
            {
                if (!newDamage.DamageDict.ContainsKey(prototype.ID))
                {
                    newDamage.DamageDict[prototype.ID] = FixedPoint2.Zero;
                }
            }
        }

        return newDamage;
    }
}