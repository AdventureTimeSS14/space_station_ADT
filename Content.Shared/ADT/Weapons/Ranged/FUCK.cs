using Robust.Shared.GameObjects;
using Robust.Shared.Reflection;
using System.Reflection;

namespace Content.Shared.Combat;

/// <summary>
/// Позволяет копировать компоненты с одной сущности на другую.
/// </summary>
public sealed class ComponentCopierSystem : EntitySystem
{
    [Dependency] private readonly IEntityManager _entityManager = default!;

    public override void Initialize()
    {
        base.Initialize();
    }

    /// <summary>
    /// Универсально копирует компонент типа T с сущности source на target.
    /// </summary>
    public void CopyComponent<T>(EntityUid source, EntityUid target) where T : Component, new()
    {
        if (!_entityManager.HasComponent<T>(source))
            return;

        var sourceComp = _entityManager.GetComponent<T>(source);
        var targetComp = _entityManager.AddComponent<T>(target);

        foreach (var field in typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (field.IsNotSerialized) continue;
            var value = field.GetValue(sourceComp);
            field.SetValue(targetComp, value);
        }
    }

    /// <summary>
    /// Копирует список компонентов по типам с source на target.
    /// </summary>
    public void CopyComponents(EntityUid source, EntityUid target, params Type[] typesToCopy)
    {
        foreach (var type in typesToCopy)
        {
            if (!_entityManager.HasComponent(source, type))
                continue;

            var sourceComp = _entityManager.GetComponent(source, type);
            var targetComp = _entityManager.AddComponent(target, type);

            foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (field.IsNotSerialized) continue;
                var value = field.GetValue(sourceComp);
                field.SetValue(targetComp, value);
            }
        }
    }
}