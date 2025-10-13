using Content.Shared.ADT.Damage.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Robust.Shared.Prototypes;
using Content.Server.Damage; // Added this using directive to resolve DamageableSystem

namespace Content.Server.ADT.Damage;

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

        // Save original container ID
        component.OriginalContainerId = damageable.DamageContainerID?.Id; // Changed to .Id (lowercase 'd') based on standard naming conventions for such wrappers

        // Create a new DamageSpecifier with only the damage types supported by the new container
        var newDamage = FilterDamageByContainer(damageable.Damage, component.ContainerId);

        // Set the new damage using the DamageableSystem
        _damageableSystem.SetDamage(uid, damageable, newDamage);

        // Update the container by completely recreating the DamageableComponent
        RecreateDamageableComponent(uid, component.ContainerId);
    }

    private void OnComponentRemove(EntityUid uid, ChangeDamageContainerComponent component, ComponentRemove args)
    {
        if (!TryComp<DamageableComponent>(uid, out var damageable) || component.OriginalContainerId == null)
            return;

        // Create a new DamageSpecifier with only the damage types supported by the original container
        var newDamage = FilterDamageByContainer(damageable.Damage, component.OriginalContainerId);

        // Set the new damage using the DamageableSystem
        _damageableSystem.SetDamage(uid, damageable, newDamage);

        // Restore the original container by completely recreating the DamageableComponent
        RecreateDamageableComponent(uid, component.OriginalContainerId);
    }

    private void RecreateDamageableComponent(EntityUid uid, string? containerId)
    {
        // Remove the current DamageableComponent
        RemComp<DamageableComponent>(uid);

        // Create and add a new DamageableComponent with the desired container
        var newComponent = new DamageableComponent();

        // Use reflection to set the private property (since it's a property with private setter)
        var propInfo = typeof(DamageableComponent).GetProperty("DamageContainerID",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance); // Changed to Public since it's public get, private set

        if (propInfo != null)
        {
            ProtoId<DamageContainerPrototype>? protoId = null;
            if (containerId != null)
            {
                protoId = new ProtoId<DamageContainerPrototype>(containerId);
            }
            propInfo.SetValue(newComponent, protoId);
        }

        AddComp(uid, newComponent);
    }

    private DamageSpecifier FilterDamageByContainer(DamageSpecifier oldDamage, string? containerId)
    {
        var newDamage = new DamageSpecifier();

        if (containerId != null &&
            _prototype.TryIndex<DamageContainerPrototype>(containerId, out var container))
        {
            // Add supported types
            foreach (var type in container.SupportedTypes)
            {
                if (oldDamage.DamageDict.TryGetValue(type, out var value))
                    newDamage.DamageDict[type] = value;
            }

            // Add supported groups
            foreach (var groupId in container.SupportedGroups)
            {
                if (!_prototype.TryIndex<DamageGroupPrototype>(groupId, out var group))
                    continue;

                foreach (var type in group.DamageTypes)
                {
                    if (oldDamage.DamageDict.TryGetValue(type, out var value))
                        newDamage.DamageDict[type] = value;
                }
            }
        }
        else
        {
            // Support all damage types if no container specified
            foreach (var (type, value) in oldDamage.DamageDict)
            {
                newDamage.DamageDict[type] = value;
            }
        }

        return newDamage;
    }
}