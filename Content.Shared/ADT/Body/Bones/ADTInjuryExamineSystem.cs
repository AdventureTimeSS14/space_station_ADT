using Content.Shared.ADT.Body.Shrapnel;
using Content.Shared.Body;
using Content.Shared.Examine;
using Robust.Shared.Containers;

namespace Content.Shared.ADT.Body.Bones;

public sealed class ADTInjuryExamineSystem : EntitySystem
{
    [Dependency] private SharedADTBoneSystem _bones = default!;
    [Dependency] private BodySystem _body = default!;
    [Dependency] private SharedContainerSystem _container = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTFracturedComponent, ExaminedEvent>(OnFracturedExamined);
        SubscribeLocalEvent<ADTEmbeddedCarrierComponent, ExaminedEvent>(OnCarrierExamined);
    }

    private void OnFracturedExamined(Entity<ADTFracturedComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        using (args.PushGroup(nameof(ADTFracturedComponent)))
        {
            foreach (var (bone, _) in _bones.EnumerateBones(ent))
            {
                if (bone.Comp.Splinted)
                {
                    args.PushMarkup(Loc.GetString("adt-fracture-examine-splint", ("target", ent.Owner), ("limb", bone.Owner)));
                    continue;
                }

                if (_bones.GetFractureType(bone.Comp) is { Open: true })
                    args.PushMarkup(Loc.GetString("adt-fracture-examine-open", ("target", ent.Owner), ("limb", bone.Owner)));
            }
        }
    }

    private void OnCarrierExamined(Entity<ADTEmbeddedCarrierComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        using (args.PushGroup(nameof(ADTEmbeddedCarrierComponent)))
        {
            foreach (var organ in _body.EnumerateOrgans<ADTEmbeddedObjectsComponent>(ent.Owner))
            {
                if (!_container.TryGetContainer(organ.Owner, ADTEmbeddedObjectsComponent.ContainerId, out var container)
                    || container.ContainedEntities.Count == 0)
                    continue;

                args.PushMarkup(Loc.GetString("adt-shrapnel-examine", ("target", ent.Owner), ("limb", organ.Owner), ("count", container.ContainedEntities.Count)));
            }
        }
    }
}
