using Content.Shared.Access;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Heretic.Effects;

// ADT: strips all access from target's ID card

public sealed partial class RemoveAccess : EntityEffectBase<RemoveAccess>
{
    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => "Removes all target access.";
}

public sealed partial class RemoveAccessEffectSystem : EntityEffectSystem<MetaDataComponent, RemoveAccess>
{
    [Dependency] private readonly SharedIdCardSystem _idCard = default!;
    [Dependency] private readonly SharedAccessSystem _access = default!;

    protected override void Effect(Entity<MetaDataComponent> entity, ref EntityEffectEvent<RemoveAccess> args)
    {
        if (!_idCard.TryFindIdCard(entity, out var id))
            return;

        _access.TrySetTags(id, new List<ProtoId<AccessLevelPrototype>>());
    }
}
