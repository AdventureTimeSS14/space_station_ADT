using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.EntityEffects;
using Solution = Content.Shared.Chemistry.Components.Solution;
using Robust.Shared.Prototypes;
using Content.Shared.Chemistry.Components.SolutionManager;

namespace Content.Shared.EntityEffects.Effects.Solution;
public sealed partial class ConvertReagentEntityEffectSystem : EntityEffectSystem<SolutionContainerManagerComponent, ConvertReagent>
{
    [Dependency] private SharedSolutionContainerSystem _solutionContainer = default!;

    protected override void Effect(Entity<SolutionContainerManagerComponent> entity, ref EntityEffectEvent<ConvertReagent> args)
    {
        var scale = args.Scale;

        var solutionName = args.Effect.SolutionName;
        Entity<SolutionComponent>? solutionEntity = null;
        Content.Shared.Chemistry.Components.Solution? solution = null;
        if (!_solutionContainer.ResolveSolution(entity.Owner, solutionName, ref solutionEntity, out solution) || solution == null)
            return;

        var soln = (solutionEntity.Value, Comp<SolutionComponent>(solutionEntity.Value));

        var removeQuantity = FixedPoint2.Max(FixedPoint2.Zero, args.Effect.RemoveAmount * scale);
        if (removeQuantity > FixedPoint2.Zero)
            _solutionContainer.RemoveReagent(soln, args.Effect.RemoveReagent, removeQuantity);

        var addQuantity = FixedPoint2.Max(FixedPoint2.Zero, args.Effect.AddAmount * scale);
        if (addQuantity > FixedPoint2.Zero)
        {
            var reagentQuantity = new ReagentQuantity(args.Effect.AddReagent, addQuantity);
            _solutionContainer.TryAddReagent(soln, reagentQuantity, out _);
        }
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class ConvertReagent : EntityEffectBase<ConvertReagent>
{
    [DataField]
    public string? SolutionName;

    [DataField(required: true)]
    public ProtoId<ReagentPrototype> RemoveReagent;

    [DataField(required: true)]
    public ProtoId<ReagentPrototype> AddReagent;

    [DataField(required: true)]
    public FixedPoint2 RemoveAmount;

    [DataField(required: true)]
    public FixedPoint2 AddAmount;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        var removeProto = prototype.Resolve(RemoveReagent, out ReagentPrototype? rProto) ? rProto.LocalizedName : "?";
        var addProto = prototype.Resolve(AddReagent, out ReagentPrototype? aProto) ? aProto.LocalizedName : "?";
        return Loc.GetString("entity-effect-guidebook-convert-reagent",
            ("remove", removeProto),
            ("removeAmount", MathF.Abs(RemoveAmount.Float())),
            ("add", addProto),
            ("addAmount", MathF.Abs(AddAmount.Float())));
    }
}
