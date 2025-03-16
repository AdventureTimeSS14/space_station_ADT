using Content.Server.ADT.Heretic.EntitySystems.PathSpecific;
using Content.Shared.ADT.Heretic.Components;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Heretic.Effects;

public sealed partial class VoidCurse : EntityEffect
{
    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => "Inflicts void curse.";

    public override void Effect(EntityEffectBaseArgs args)
    {
        args.EntityManager.System<VoidCurseSystem>().DoCurse(args.TargetEntity);
    }
}
