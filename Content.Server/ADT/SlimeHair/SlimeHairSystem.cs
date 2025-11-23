using Content.Server.Actions;
using Content.Server.DoAfter;
using Content.Server.Humanoid;
using Content.Shared.ADT.SlimeHair;
using Content.Shared.Corvax.TTS;
using Content.Shared.DoAfter;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Interaction;
using Content.Shared.Preferences;
using Content.Shared.UserInterface;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using System.Linq;

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
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MidroundCustomizationComponent, ActivatableUIOpenAttemptEvent>(OnOpenUIAttempt);

        Subs.BuiEvents<MidroundCustomizationComponent>(SlimeHairUiKey.Key, subs =>
        {
            subs.Event<SlimeHairSelectMessage>(OnSlimeHairSelect);
            subs.Event<SlimeHairChangeColorMessage>(OnTrySlimeHairChangeColor);
            subs.Event<SlimeHairAddSlotMessage>(OnTrySlimeHairAddSlot);
            subs.Event<SlimeHairRemoveSlotMessage>(OnTrySlimeHairRemoveSlot);
            subs.Event<SlimeHairChangeVoiceMessage>(OnTrySlimeHairChangeVoice);
        });

        SubscribeLocalEvent<MidroundCustomizationComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<MidroundCustomizationComponent, ComponentShutdown>(OnShutdown);

        SubscribeLocalEvent<MidroundCustomizationComponent, SlimeHairSelectDoAfterEvent>(OnSelectSlotDoAfter);
        SubscribeLocalEvent<MidroundCustomizationComponent, SlimeHairChangeColorDoAfterEvent>(OnChangeColorDoAfter);
        SubscribeLocalEvent<MidroundCustomizationComponent, SlimeHairRemoveSlotDoAfterEvent>(OnRemoveSlotDoAfter);
        SubscribeLocalEvent<MidroundCustomizationComponent, SlimeHairAddSlotDoAfterEvent>(OnAddSlotDoAfter);
        SubscribeLocalEvent<MidroundCustomizationComponent, SlimeHairChangeVoiceDoAfterEvent>(OnChangeVoiceDoAfter);

        InitializeSlimeAbilities();

    }

    private void OnOpenUIAttempt(EntityUid uid, MidroundCustomizationComponent mirror, ActivatableUIOpenAttemptEvent args)
    {
        if (!HasComp<HumanoidAppearanceComponent>(uid))
            args.Cancel();
    }

    private void OnSlimeHairSelect(EntityUid uid, MidroundCustomizationComponent component, SlimeHairSelectMessage message)
    {
        _doAfterSystem.Cancel(component.DoAfter);
        component.DoAfter = null;

        var doAfter = new SlimeHairSelectDoAfterEvent()
        {
            Category = message.Category,
            Slot = message.Slot,
            Marking = message.Marking,
        };

        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, uid, component.SelectSlotTime, doAfter, uid, target: uid, used: uid)
        {
            DistanceThreshold = SharedInteractionSystem.InteractionRange,
            BreakOnDamage = true,
        }, out var doAfterId);

        component.DoAfter = doAfterId;
    }

    private void OnSelectSlotDoAfter(EntityUid uid, MidroundCustomizationComponent component, SlimeHairSelectDoAfterEvent args)
    {
        if (args.Handled || args.Target == null || args.Cancelled)
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

    private void OnTrySlimeHairChangeColor(EntityUid uid, MidroundCustomizationComponent component, SlimeHairChangeColorMessage message)
    {
        _doAfterSystem.Cancel(component.DoAfter);
        component.DoAfter = null;

        var doAfter = new SlimeHairChangeColorDoAfterEvent()
        {
            Category = message.Category,
            Slot = message.Slot,
            Colors = message.Colors,
        };

        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, uid, component.ChangeSlotTime, doAfter, uid, target: uid, used: uid)
        {
            BreakOnDamage = true,
        }, out var doAfterId);

        component.DoAfter = doAfterId;
    }

    private void OnChangeColorDoAfter(EntityUid uid, MidroundCustomizationComponent component, SlimeHairChangeColorDoAfterEvent args)
    {
        if (args.Handled || args.Target == null || args.Cancelled)
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

    private void OnTrySlimeHairRemoveSlot(EntityUid uid, MidroundCustomizationComponent component, SlimeHairRemoveSlotMessage message)
    {
        _doAfterSystem.Cancel(component.DoAfter);
        component.DoAfter = null;

        var doAfter = new SlimeHairRemoveSlotDoAfterEvent()
        {
            Category = message.Category,
            Slot = message.Slot,
        };

        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, uid, component.RemoveSlotTime, doAfter, uid, target: uid, used: uid)
        {
            DistanceThreshold = SharedInteractionSystem.InteractionRange,
            BreakOnDamage = true,
        }, out var doAfterId);

        component.DoAfter = doAfterId;
    }

    private void OnRemoveSlotDoAfter(EntityUid uid, MidroundCustomizationComponent component, SlimeHairRemoveSlotDoAfterEvent args)
    {
        if (args.Handled || args.Target == null || args.Cancelled)
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

        _humanoid.RemoveMarking(uid, category, args.Slot);

        _audio.PlayPvs(component.ChangeHairSound, uid);
        UpdateInterface(uid, component);
    }

    private void OnTrySlimeHairAddSlot(EntityUid uid, MidroundCustomizationComponent component, SlimeHairAddSlotMessage message)
    {
        if (message.Actor == null)
            return;

        _doAfterSystem.Cancel(component.DoAfter);
        component.DoAfter = null;

        var doAfter = new SlimeHairAddSlotDoAfterEvent()
        {
            Category = message.Category,
        };

        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, message.Actor, component.AddSlotTime, doAfter, uid, target: uid, used: uid)
        {
            BreakOnDamage = true,
        }, out var doAfterId);

        component.DoAfter = doAfterId;
        _audio.PlayPvs(component.ChangeHairSound, uid);
    }

    private void OnAddSlotDoAfter(EntityUid uid, MidroundCustomizationComponent component, SlimeHairAddSlotDoAfterEvent args)
    {
        if (args.Handled || args.Target == null || args.Cancelled || !TryComp(uid, out HumanoidAppearanceComponent? humanoid))
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

    private void OnTrySlimeHairChangeVoice(EntityUid uid, MidroundCustomizationComponent component, SlimeHairChangeVoiceMessage message)
    {
        _doAfterSystem.Cancel(component.DoAfter);
        component.DoAfter = null;

        var doAfter = new SlimeHairChangeVoiceDoAfterEvent()
        {
            Voice = message.Voice,
        };

        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, uid, component.ChangeVoiceTime, doAfter, uid, target: uid, used: uid)
        {
            DistanceThreshold = SharedInteractionSystem.InteractionRange,
            BreakOnDamage = true,
        }, out var doAfterId);

        component.DoAfter = doAfterId;
    }

    private void OnChangeVoiceDoAfter(EntityUid uid, MidroundCustomizationComponent component, SlimeHairChangeVoiceDoAfterEvent args)
    {
        if (args.Handled || args.Target == null || args.Cancelled)
            return;

        if (args.Target)
            return;

        if (!TryComp<HumanoidAppearanceComponent>(args.Target.Value, out var humanoid))
            return;

        if (!_proto.TryIndex<TTSVoicePrototype>(args.Voice, out var proto) || !HumanoidCharacterProfile.CanHaveVoice(proto, humanoid.Sex))
            return;

        _humanoid.SetTTSVoice(args.Target.Value, args.Voice, humanoid);

        UpdateInterface(uid, component);
    }

    private void UpdateInterface(EntityUid uid, MidroundCustomizationComponent component)
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
            humanoid.Sex,
            humanoid.Voice,
            hair,
            humanoid.MarkingSet.PointsLeft(MarkingCategories.Hair) + hair.Count,
            facialHair,
            humanoid.MarkingSet.PointsLeft(MarkingCategories.FacialHair) + facialHair.Count);

        _uiSystem.SetUiState(uid, SlimeHairUiKey.Key, state);
    }

    private void OnMapInit(EntityUid uid, MidroundCustomizationComponent component, MapInitEvent args)
    {
        _action.AddAction(uid, ref component.ActionEntity, component.Action);
    }

    private void OnShutdown(EntityUid uid, MidroundCustomizationComponent component, ComponentShutdown args)
    {
        _action.RemoveAction(uid, component.ActionEntity);
    }

}
