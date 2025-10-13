using Content.Shared.ADT.Damage.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Damage.Systems;

public sealed class ChangeDamageContainerSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;

    private static readonly System.Reflection.PropertyInfo? DamageContainerIdProperty =
        typeof(DamageableComponent).GetProperty("DamageContainerID",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

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

        RecreateDamageableComponent(uid, component.ContainerId, damageable);
    }

    private void OnComponentRemove(EntityUid uid, ChangeDamageContainerComponent component, ComponentRemove args)
    {
        if (!TryComp<DamageableComponent>(uid, out var damageable) || component.OriginalContainerId == null)
            return;

        RecreateDamageableComponent(uid, component.OriginalContainerId, damageable);
    }

    private void RecreateDamageableComponent(EntityUid uid, string? containerId, DamageableComponent oldDamageable)
    {
        var oldDamage = oldDamageable.Damage;

        RemComp<DamageableComponent>(uid);

        var newComponent = new DamageableComponent();

        if (DamageContainerIdProperty != null)
        {
            ProtoId<DamageContainerPrototype>? protoId = null;
            if (containerId != null)
            {
                protoId = new ProtoId<DamageContainerPrototype>(containerId);
            }
            DamageContainerIdProperty.SetValue(newComponent, protoId);
        }

        AddComp(uid, newComponent);

        if (TryComp<DamageableComponent>(uid, out var newDamageable))
        {
            var filteredDamage = FilterDamageByContainer(oldDamage, containerId);
            _damageableSystem.SetDamage(uid, newDamageable, filteredDamage);
        }
    }

    private DamageSpecifier FilterDamageByContainer(DamageSpecifier oldDamage, string? containerId)
    {
        var newDamage = new DamageSpecifier();

        if (containerId != null && _prototype.TryIndex<DamageContainerPrototype>(containerId, out var container))
        {
            var supportedTypes = new HashSet<string>(container.SupportedTypes);

            foreach (var groupId in container.SupportedGroups)
            {
                if (_prototype.TryIndex<DamageGroupPrototype>(groupId, out var group))
                {
                    foreach (var type in group.DamageTypes)
                    {
                        supportedTypes.Add(type);
                    }
                }
            }

            foreach (var (type, value) in oldDamage.DamageDict)
            {
                if (supportedTypes.Contains(type))
                {
                    newDamage.DamageDict[type] = value;
                }
            }
        }
        else
        {
            newDamage = new DamageSpecifier(oldDamage);
        }

        return newDamage;
    }
}