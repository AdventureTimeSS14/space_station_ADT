using System.Linq;
using Content.Shared.CombatMode;
using Content.Shared.Actions;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Humanoid;
using Content.Shared.Popups;

namespace Content.Shared.ADT.Combat;

public abstract class SharedPrepareActionSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PrepareActionComponent, ComponentInit>(OnMapInit);
        SubscribeLocalEvent<PrepareActionComponent, MeleeHitEvent>(OnMeleeHit);
        SubscribeLocalEvent<PrepareActionComponent, PrepareMoveEvent>(OnPrepareAction);
        SubscribeLocalEvent<PrepareActionComponent, ComponentShutdown>(OnShutdown);
    }
    private void OnMapInit(EntityUid uid, PrepareActionComponent comp, ComponentInit args)
    {
        if (HasComp<ComboComponent>(uid) && !comp.CanBeUsedWithCombo)
            return;
        foreach (var actionId in comp.BaseCombatMoves)
        {
            var actions = _actions.AddAction(uid, actionId);
            if (actions != null)
                comp.CombatMoveEntities.Add(actions.Value);
        }
    }

    private void OnMeleeHit(EntityUid uid, PrepareActionComponent comp, MeleeHitEvent args)
    {
        if (comp.PreparedMove == null)
            return;
        if (!args.IsHit || !args.HitEntities.Any())
            return;

        if (!TryComp<HumanoidAppearanceComponent>(args.HitEntities[0], out _))
            return;
        UseEventOnTarget(uid, args.HitEntities[0], comp.PreparedMove);
        comp.PreparedMove = null;
    }
    private void OnPrepareAction(EntityUid uid, PrepareActionComponent comp, PrepareMoveEvent args)
    {
        comp.PreparedMove = args.ComboEvents;
        _popupSystem.PopupCursor(Loc.GetString("move-ready", ("action", args.Name)), args.Performer);
        foreach (var action in comp.CombatMoveEntities)
        {
            _actions.StartUseDelay(action);
        }
    }
    private void OnShutdown(Entity<PrepareActionComponent> ent, ref ComponentShutdown args)
    {
        foreach (var action in ent.Comp.CombatMoveEntities)
        {
            _actions.RemoveAction(action);
        }
    }
    public void UseEventOnTarget(EntityUid user, EntityUid target, List<IComboEffect> combo)
    {
        foreach (var comboEvent in combo)
        {
            comboEvent.DoEffect(user, target, EntityManager);
        }
    }
}
