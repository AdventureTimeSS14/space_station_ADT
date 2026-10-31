using System.Diagnostics.CodeAnalysis;
using Content.Shared.Body;
using Content.Shared.Implants.Components;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Surgery.Prototypes.Conditions;

[Serializable, NetSerializable]
public sealed partial class HasImplantCondition : SurgeryStepCondition
{
    [DataField]
    public List<EntProtoId> Include = new();

    [DataField]
    public List<EntProtoId> Exclude = new();

    [DataField]
    public ProtoId<OrganCategoryPrototype>? OrganId;

    public override bool Check(EntityUid patient, IEntityManager entityManager)
    {
        if (!TryGetImplanted(patient, OrganId, entityManager, out var implanted))
            return false;

        foreach (var implant in implanted.ImplantContainer.ContainedEntities)
        {
            if (CanExtract(implant, implanted.ImplantContainer, entityManager, Include, Exclude))
                return true;
        }

        return false;
    }

    public static bool TryGetImplanted(
        EntityUid patient,
        ProtoId<OrganCategoryPrototype>? organId,
        IEntityManager entityManager,
        [NotNullWhen(true)] out ImplantedComponent? implanted)
    {
        implanted = null;

        if (organId == null)
            return entityManager.TryGetComponent(patient, out implanted);

        foreach (var organ in entityManager.System<BodySystem>().EnumerateOrgans<ImplantedComponent>(patient))
        {
            if (organ.Comp1.Category != organId)
                continue;

            implanted = organ.Comp2;
            return true;
        }

        return false;
    }

    public static bool CanExtract(
        EntityUid implant,
        BaseContainer container,
        IEntityManager entityManager,
        List<EntProtoId> include,
        List<EntProtoId> exclude)
    {
        if (entityManager.TryGetComponent<SubdermalImplantComponent>(implant, out var subdermal) && subdermal.Permanent)
            return false;

        var proto = entityManager.GetComponent<MetaDataComponent>(implant).EntityPrototype?.ID;
        if (include.Count > 0 && (proto == null || !include.Contains(proto)))
            return false;

        if (proto != null && exclude.Contains(proto))
            return false;

        return entityManager.System<SharedContainerSystem>().CanRemove(implant, container);
    }
}
