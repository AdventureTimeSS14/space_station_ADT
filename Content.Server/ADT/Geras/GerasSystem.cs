using Content.Server.Polymorph.Systems;
using Content.Shared.Zombies;
using Content.Server.Actions;
using Content.Server.Popups;
using Content.Shared.ADT.Geras;
using Robust.Shared.Player;
using Content.Shared.Humanoid;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.ActionBlocker;
using Content.Shared.Item;
using Content.Shared.Hands;

namespace Content.Server.ADT.Geras;

public sealed class GerasSystem : SharedGerasSystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly PolymorphSystem _polymorphSystem = default!;
    [Dependency] private readonly ActionsSystem _actionsSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<GerasComponent, MorphIntoGeras>(OnMorphIntoGeras);
        SubscribeLocalEvent<GerasComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<GerasComponent, EntityZombifiedEvent>(OnZombification);
    }

    private void OnZombification(EntityUid uid, GerasComponent component, EntityZombifiedEvent args)
    {
        _actionsSystem.RemoveAction(uid, component.GerasActionEntity);
    }

    private void OnMapInit(EntityUid uid, GerasComponent component, MapInitEvent args)
    {
        // try to add geras action to non geras
        if (!component.NoAction)
        {
            _actionsSystem.AddAction(uid, ref component.GerasActionEntity, component.GerasAction);
        }
    }

    private void OnMorphIntoGeras(EntityUid uid, GerasComponent component, MorphIntoGeras args)
    {
        if (HasComp<ZombieComponent>(uid))
            return;

        if (!_actionBlocker.CanInteract(uid, null) || _mobState.IsDead(uid) || _mobState.IsIncapacitated(uid))
        {
            _popupSystem.PopupEntity(Loc.GetString("geras-popup-cant-use"), uid, uid);
            return;
        }

        var ent = _polymorphSystem.PolymorphEntity(uid, component.GerasPolymorphId);

        if (!ent.HasValue)
            return;

        var skinColor = Color.Green;

        if (TryComp<HumanoidAppearanceComponent>(uid, out var humanComp))
        {
            skinColor = humanComp.SkinColor;
        }

        if (TryComp<AppearanceComponent>(ent, out var appearanceComp))
        {
            _appearance.SetData(ent.Value, GeraColor.Color, skinColor, appearanceComp);
        }

        _popupSystem.PopupEntity(Loc.GetString("geras-popup-morph-message-others", ("entity", ent.Value)), ent.Value, Filter.PvsExcept(ent.Value), true);
        _popupSystem.PopupEntity(Loc.GetString("geras-popup-morph-message-user"), ent.Value, ent.Value);

        args.Handled = true;
    }
}
