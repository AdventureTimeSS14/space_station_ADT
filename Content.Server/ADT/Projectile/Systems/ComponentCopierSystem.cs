using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using System;
using System.Reflection;

namespace Content.Server.ADT.Projectile.Systems;

/// <summary>
/// Позволяет копировать компоненты с одной сущности на другую (только на сервере).
/// </summary>
public sealed class ComponentCopierSystem : EntitySystem
{
    [Dependency] private readonly IEntityManager _entityManager = default!;

    /// <summary>
    /// Копирует компонент типа T с сущности source на target.
    /// </summary>
    public void CopyComponent<T>(EntityUid source, EntityUid target) where T : Component, new()
    {
        if (!_entityManager.HasComponent<T>(source))
            return;

        if (_entityManager.HasComponent<T>(target))
            return;

        var sourceComp = _entityManager.GetComponent<T>(source);
        var targetComp = _entityManager.AddComponent<T>(target);

        if (targetComp == null)
            return;

        CopyFields(typeof(T), sourceComp, targetComp);
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

            if (_entityManager.HasComponent(target, type))
                continue;

            var sourceComp = _entityManager.GetComponent(source, type);
            var targetComp = _entityManager.AddComponent(target, type);

            if (targetComp == null)
                continue;

            CopyFields(type, sourceComp, targetComp);
        }
    }

    /// <summary>
    /// Копирует все сериализуемые поля между двумя компонентами одного типа.
    /// </summary>
    private void CopyFields(Type type, object source, object target)
    {
        foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (field.IsNotSerialized)
                continue;

            var value = field.GetValue(source);
            field.SetValue(target, value);
        }
    }
}