using Content.Shared.CombatMode.Pacification;
using Content.Shared.Popups;
using Content.Shared.Standing;
using Content.Shared.StatusEffect;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects;

/// <summary>
/// Вызывает дрожь-спазм в руках - роняет то, что держит сущность, и на короткое время накладывает пацифизм.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class TremorSpasmEffectSystem : EntityEffectSystem<StatusEffectsComponent, TremorSpasm>
{
    [Dependency] private readonly StatusEffectsSystem _status = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    protected override void Effect(Entity<StatusEffectsComponent> entity, ref EntityEffectEvent<TremorSpasm> args)
    {
        var dropEv = new DropHandItemsEvent();
        RaiseLocalEvent(entity.Owner, ref dropEv);

        _status.TryAddStatusEffect<PacifiedComponent>(entity, "Pacified", args.Effect.PacifyDuration, true, entity.Comp);

        var selfMessage = Loc.GetString("narcotic-effect-hand-tremor");
        var othersMessage = Loc.GetString("narcotic-effect-hand-tremor-others", ("entity", entity.Owner));
        _popup.PopupPredicted(selfMessage, othersMessage, entity.Owner, entity.Owner, PopupType.LargeCaution);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class TremorSpasm : EntityEffectBase<TremorSpasm>
{
    /// <summary>
    /// Длительность состояния пацифизма, накладываемого при спазме рук.
    /// </summary>
    [DataField]
    public TimeSpan PacifyDuration = TimeSpan.FromSeconds(4);

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) =>
        Loc.GetString("entity-effect-guidebook-tremor-spasm", ("chance", Probability), ("time", PacifyDuration.TotalSeconds));
}


/*
                      (x_x)
                       /|\
                       / \

    РДМ отменяется — руки трясутся, ломает ужасно.
*/
