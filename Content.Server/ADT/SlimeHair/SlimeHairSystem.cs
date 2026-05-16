using System.Linq;
using Content.Server.DoAfter;
using Content.Server.Humanoid;
using Content.Shared.UserInterface;
using Content.Shared.DoAfter;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Interaction;
using Content.Shared.ADT.SlimeHair;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Content.Server.Actions;

namespace Content.Server.ADT.SlimeHair;

/// <summary>
/// Allows humanoids to change their appearance mid-round.
/// </summary>

// TODO: Исправить проблему с генокрадом
public sealed partial class SlimeHairSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly MarkingManager _markings = default!;
    [Dependency] private readonly HumanoidAppearanceSystem _humanoid = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly ActionsSystem _action = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SlimeHairComponent, ActivatableUIOpenAttemptEvent>(OnOpenUIAttempt);

        Subs.BuiEvents<SlimeHairComponent>(SlimeHairUiKey.Key, subs =>
        {
            subs.Event<BoundUIClosedEvent>(OnUIClosed);
            subs.Event<SlimeHairSelectMessage>(OnSlimeHairSelect);
            subs.Event<SlimeHairChangeColorMessage>(OnTrySlimeHairChangeColor);
            subs.Event<SlimeHairAddSlotMessage>(OnTrySlimeHairAddSlot);
            subs.Event<SlimeHairRemoveSlotMessage>(OnTrySlimeHairRemoveSlot);
        });

        SubscribeLocalEvent<SlimeHairComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<SlimeHairComponent, ComponentShutdown>(OnShutdown);

        SubscribeLocalEvent<SlimeHairComponent, SlimeHairSelectDoAfterEvent>(OnSelectSlotDoAfter);
        SubscribeLocalEvent<SlimeHairComponent, SlimeHairChangeColorDoAfterEvent>(OnChangeColorDoAfter);
        SubscribeLocalEvent<SlimeHairComponent, SlimeHairRemoveSlotDoAfterEvent>(OnRemoveSlotDoAfter);
        SubscribeLocalEvent<SlimeHairComponent, SlimeHairAddSlotDoAfterEvent>(OnAddSlotDoAfter);

        InitializeSlimeAbilities();

    }

    private void OnOpenUIAttempt(EntityUid uid, SlimeHairComponent mirror, ActivatableUIOpenAttemptEvent args)
    {
        if (!HasComp<HumanoidAppearanceComponent>(uid))
            args.Cancel();
    }

    private void OnSlimeHairSelect(EntityUid uid, SlimeHairComponent component, SlimeHairSelectMessage message)
    {
        if (component.Target is not { } target)
            return;

        _doAfterSystem.Cancel(component.DoAfter);
        component.DoAfter = null;

        var doAfter = new SlimeHairSelectDoAfterEvent()
        {
            Category = message.Category,
            Slot = message.Slot,
            Marking = message.Marking,
        };

        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, component.Owner, component.SelectSlotTime, doAfter, uid, target: target, used: uid)
        {
            DistanceThreshold = SharedInteractionSystem.InteractionRange,
            BreakOnDamage = true,
        }, out var doAfterId);

        component.DoAfter = doAfterId;
    }

    private void OnSelectSlotDoAfter(EntityUid uid, SlimeHairComponent component, SlimeHairSelectDoAfterEvent args)
    {
        if (args.Handled || args.Target == null || args.Cancelled)
            return;

        if (component.Target != args.Target)
            return;

        MarkingCategories category;

        switch (args.Category)
        {
            case SlimeHairCategory.Hair:
                category = MarkingCategories.Hair;
                break;
            case SlimeHairCategory.FacialHair:
                category = MarkingCategories.FacialHair;
                break;
            default:
                return;
        }

        _humanoid.SetMarkingId(uid, category, args.Slot, args.Marking);

        UpdateInterface(uid, component);
    }

    private void OnTrySlimeHairChangeColor(EntityUid uid, SlimeHairComponent component, SlimeHairChangeColorMessage message)
    {
        if (component.Target is not { } target)
            return;

        _doAfterSystem.Cancel(component.DoAfter);
        component.DoAfter = null;

        var doAfter = new SlimeHairChangeColorDoAfterEvent()
        {
            Category = message.Category,
            Slot = message.Slot,
            Colors = message.Colors,
        };

        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, component.Owner, component.ChangeSlotTime, doAfter, uid, target: target, used: uid)
        {
            BreakOnDamage = true,
        }, out var doAfterId);

        component.DoAfter = doAfterId;
    }
    private void OnChangeColorDoAfter(EntityUid uid, SlimeHairComponent component, SlimeHairChangeColorDoAfterEvent args)
    {
        if (args.Handled || args.Target == null || args.Cancelled)
            return;

        if (component.Target != args.Target)
            return;

        MarkingCategories category;
        switch (args.Category)
        {
            case SlimeHairCategory.Hair:
                category = MarkingCategories.Hair;
                break;
            case SlimeHairCategory.FacialHair:
                category = MarkingCategories.FacialHair;
                break;
            default:
                return;
        }

        _humanoid.SetMarkingColor(uid, category, args.Slot, args.Colors);

        // using this makes the UI feel like total ass
        // que
        // UpdateInterface(uid, component.Target, message.Session);
    }

    private void OnTrySlimeHairRemoveSlot(EntityUid uid, SlimeHairComponent component, SlimeHairRemoveSlotMessage message)
    {
        if (component.Target is not { } target)
            return;

        _doAfterSystem.Cancel(component.DoAfter);
        component.DoAfter = null;

        var doAfter = new SlimeHairRemoveSlotDoAfterEvent()
        {
            Category = message.Category,
            Slot = message.Slot,
        };

        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, component.Owner, component.RemoveSlotTime, doAfter, uid, target: target, used: uid)
        {
            DistanceThreshold = SharedInteractionSystem.InteractionRange,
            BreakOnDamage = true,
        }, out var doAfterId);

        component.DoAfter = doAfterId;
    }

    private void OnRemoveSlotDoAfter(EntityUid uid, SlimeHairComponent component, SlimeHairRemoveSlotDoAfterEvent args)
    {
        if (args.Handled || args.Target == null || args.Cancelled)
            return;

        if (component.Target != args.Target)
            return;

        MarkingCategories category;

        switch (args.Category)
        {
            case SlimeHairCategory.Hair:
                category = MarkingCategories.Hair;
                break;
            case SlimeHairCategory.FacialHair:
                category = MarkingCategories.FacialHair;
                break;
            default:
                return;
        }

        _humanoid.RemoveMarking(component.Target.Value, category, args.Slot);

        _audio.PlayPvs(component.ChangeHairSound, uid);
        UpdateInterface(uid, component);
    }

    private void OnTrySlimeHairAddSlot(EntityUid uid, SlimeHairComponent component, SlimeHairAddSlotMessage message)
    {
        if (component.Target == null)
            return;

        if (message.Actor == null)
            return;

        _doAfterSystem.Cancel(component.DoAfter);
        component.DoAfter = null;

        var doAfter = new SlimeHairAddSlotDoAfterEvent()
        {
            Category = message.Category,
        };

        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, message.Actor, component.AddSlotTime, doAfter, uid, target: component.Target.Value, used: uid)
        {
            BreakOnDamage = true,
        }, out var doAfterId);

        component.DoAfter = doAfterId;
        _audio.PlayPvs(component.ChangeHairSound, uid);
    }
    private void OnAddSlotDoAfter(EntityUid uid, SlimeHairComponent component, SlimeHairAddSlotDoAfterEvent args)
    {
        if (args.Handled || args.Target == null || args.Cancelled || !TryComp(component.Target, out HumanoidAppearanceComponent? humanoid))
            return;

        MarkingCategories category;

        switch (args.Category)
        {
            case SlimeHairCategory.Hair:
                category = MarkingCategories.Hair;
                break;
            case SlimeHairCategory.FacialHair:
                category = MarkingCategories.FacialHair;
                break;
            default:
                return;
        }

        var marking = _markings.MarkingsByCategoryAndSpecies(category, humanoid.Species).Keys.FirstOrDefault();

        if (string.IsNullOrEmpty(marking))
            return;

        _audio.PlayPvs(component.ChangeHairSound, uid);
        _humanoid.AddMarking(uid, marking, Color.Black);

        UpdateInterface(uid, component);

    }

    private void UpdateInterface(EntityUid uid, SlimeHairComponent component)
    {
        if (!TryComp<HumanoidAppearanceComponent>(uid, out var humanoid))
            return;

        var hair = humanoid.MarkingSet.TryGetCategory(MarkingCategories.Hair, out var hairMarkings)
            ? new List<Marking>(hairMarkings)
            : new();

        var facialHair = humanoid.MarkingSet.TryGetCategory(MarkingCategories.FacialHair, out var facialHairMarkings)
            ? new List<Marking>(facialHairMarkings)
            : new();

        var state = new SlimeHairUiState(
            humanoid.Species,
            hair,
            humanoid.MarkingSet.PointsLeft(MarkingCategories.Hair) + hair.Count,
            facialHair,
            humanoid.MarkingSet.PointsLeft(MarkingCategories.FacialHair) + facialHair.Count);

        component.Target = uid;
        _uiSystem.SetUiState(uid, SlimeHairUiKey.Key, state);
    }

    private void OnUIClosed(Entity<SlimeHairComponent> ent, ref BoundUIClosedEvent args)
    {
        ent.Comp.Target = null;
    }

    private void OnMapInit(EntityUid uid, SlimeHairComponent component, MapInitEvent args)
    {
        _action.AddAction(uid, ref component.ActionEntity, component.Action);
    }
    private void OnShutdown(EntityUid uid, SlimeHairComponent component, ComponentShutdown args)
    {
        _action.RemoveAction(uid, component.ActionEntity);
    }

}
