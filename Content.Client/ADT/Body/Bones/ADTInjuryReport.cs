using Content.Shared.ADT.Body.Bones;
using Content.Shared.ADT.Body.Shrapnel;
using Content.Shared.Body;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Client.ADT.Body.Bones;

public static class ADTInjuryReport
{
    public static List<string> GetLines(IEntityManager entMan, IPrototypeManager proto, EntityUid target)
    {
        var lines = new List<string>();
        var body = entMan.System<BodySystem>();
        var containers = entMan.System<SharedContainerSystem>();

        foreach (var organ in body.EnumerateOrgans<OrganComponent>(target))
        {
            var name = entMan.GetComponent<MetaDataComponent>(organ.Owner).EntityName;

            if (entMan.TryGetComponent<ADTBoneComponent>(organ.Owner, out var bone)
                && bone.Fracture is { } fracture
                && proto.TryIndex(fracture, out var type))
            {
                var key = bone.Splinted ? "adt-health-analyzer-fracture-splinted" : "adt-health-analyzer-fracture";
                lines.Add(Loc.GetString(key, ("limb", name), ("type", Loc.GetString(type.Name))));
            }

            if (containers.TryGetContainer(organ.Owner, ADTEmbeddedObjectsComponent.ContainerId, out var container)
                && container.ContainedEntities.Count > 0)
                lines.Add(Loc.GetString("adt-health-analyzer-shrapnel", ("limb", name), ("count", container.ContainedEntities.Count)));
        }

        return lines;
    }
}
