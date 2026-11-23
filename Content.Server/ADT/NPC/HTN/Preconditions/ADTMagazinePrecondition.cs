using Content.Server.NPC;
using Content.Server.NPC.HTN.Preconditions;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.NPC.HTN.Preconditions;

public sealed partial class ADTMagazinePrecondition : HTNPrecondition
{
    [Dependency] private IEntityManager _entManager = default!;

    [DataField]
    public ADTNpcWeaponRole Role = ADTNpcWeaponRole.Main;

    [DataField(required: true)]
    public List<EntProtoId> Magazines = new();

    [DataField]
    public bool Invert;

    public override bool IsMet(NPCBlackboard blackboard)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        return HasMagazine(owner) ^ Invert;
    }

    private bool HasMagazine(EntityUid owner)
    {
        var gear = _entManager.System<ADTNpcGearSystem>();

        if (gear.Resolve(owner, Role) is not { } found ||
            !gear.IsMagazineGun(found.Item, out var slot) ||
            slot?.Item is not { } magazine ||
            _entManager.GetComponent<MetaDataComponent>(magazine).EntityPrototype?.ID is not { } id)
        {
            return false;
        }

        foreach (var proto in Magazines)
        {
            if (proto.Id == id)
                return true;
        }

        return false;
    }
}
