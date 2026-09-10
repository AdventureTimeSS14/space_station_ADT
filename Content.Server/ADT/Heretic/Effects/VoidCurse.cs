using Content.Server.ADT.Heretic.EntitySystems.PathSpecific;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Heretic.Effects;

// ADT: applies void curse

public sealed partial class VoidCurse : EntityEffectBase<VoidCurse>
{
    [DataField]
    public int Stacks = 1;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => "Inflicts void curse.";
}

public sealed partial class VoidCurseEffectSystem : EntityEffectSystem<MetaDataComponent, VoidCurse>
{
    [Dependency] private readonly VoidCurseSystem _voidCurse = default!;

    protected override void Effect(Entity<MetaDataComponent> entity, ref EntityEffectEvent<VoidCurse> args)
    {
        _voidCurse.DoCurse(entity, args.Effect.Stacks);
    }
}
