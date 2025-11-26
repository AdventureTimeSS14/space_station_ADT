using Content.Server.Actions;
using Content.Server.DoAfter;
using Content.Server.Humanoid;
using Content.Shared.ADT.MidroundCustomization;
using Content.Shared.ADT.SpeechBarks;
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

namespace Content.Server.ADT.MidroundCustomization;

/// <summary>
/// Allows humanoids to change their appearance mid-round.
/// </summary>

// TODO: Исправить проблему с генокрадом
public sealed partial class MidroundCustomizationSystem : EntitySystem
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

        Subs.BuiEvents<MidroundCustomizationComponent>(MidroundCustomizationUiKey.Key, subs =>
        {
            subs.Event<MidroundCustomizationMarkingSelectMessage>(OnSlimeHairSelect);
            subs.Event<MidroundCustomizationChangeColorMessage>(OnTrySlimeHairChangeColor);
            subs.Event<MidroundCustomizationAddSlotMessage>(OnTrySlimeHairAddSlot);
            subs.Event<MidroundCustomizationRemoveSlotMessage>(OnTrySlimeHairRemoveSlot);
            subs.Event<MidroundCustomizationChangeVoiceMessage>(OnTrySlimeHairChangeVoice);
            subs.Event<MidroundCustomizationChangeBarkProtoMessage>(OnTryChangeBarkProto);
            subs.Event<MidroundCustomizationChangeBarkPitchMessage>(OnTryChangeBarkPitch);
            subs.Event<MidroundCustomizationChangeBarkMinVarMessage>(OnTryChangeBarkMinVar);
            subs.Event<MidroundCustomizationChangeBarkMaxVarMessage>(OnTryChangeBarkMaxVar);
        });

        SubscribeLocalEvent<MidroundCustomizationComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<MidroundCustomizationComponent, ComponentShutdown>(OnShutdown);

        SubscribeLocalEvent<MidroundCustomizationComponent, SlimeHairSelectDoAfterEvent>(OnSelectSlotDoAfter);
        SubscribeLocalEvent<MidroundCustomizationComponent, SlimeHairChangeColorDoAfterEvent>(OnChangeColorDoAfter);
        SubscribeLocalEvent<MidroundCustomizationComponent, SlimeHairRemoveSlotDoAfterEvent>(OnRemoveSlotDoAfter);
        SubscribeLocalEvent<MidroundCustomizationComponent, SlimeHairAddSlotDoAfterEvent>(OnAddSlotDoAfter);
        SubscribeLocalEvent<MidroundCustomizationComponent, SlimeHairChangeVoiceDoAfterEvent>(OnChangeVoiceDoAfter);
        SubscribeLocalEvent<MidroundCustomizationComponent, SlimeHairChangeBarkProtoDoAfterEvent>(OnChangeBarkProtoDoAfter);
        SubscribeLocalEvent<MidroundCustomizationComponent, SlimeHairChangeBarkPitchDoAfterEvent>(OnChangeBarkPitchDoAfter);
        SubscribeLocalEvent<MidroundCustomizationComponent, SlimeHairChangeBarkMinVarDoAfterEvent>(OnChangeBarkMinVarDoAfter);
        SubscribeLocalEvent<MidroundCustomizationComponent, SlimeHairChangeBarkMaxVarDoAfterEvent>(OnChangeBarkMaxVarDoAfter);

        InitializeAbilities();

    }

    private void OnOpenUIAttempt(EntityUid uid, MidroundCustomizationComponent mirror, ActivatableUIOpenAttemptEvent args)
    {
        if (!HasComp<HumanoidAppearanceComponent>(uid))
            args.Cancel();
    }

    private void OnSlimeHairSelect(EntityUid uid, MidroundCustomizationComponent component, MidroundCustomizationMarkingSelectMessage message)
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

        if (!TryComp<HumanoidAppearanceComponent>(uid, out var humanoid))
            return;

        _audio.PlayPvs(component.ChangeMarkingSound, uid);
        _humanoid.SetMarkingId(uid, args.Category, args.Slot, args.Marking, force: false, defaultColor: component.DefaultSkinColoring ? humanoid.SkinColor : Color.Gray);
        UpdateInterface(uid, component);
    }

    private void OnTrySlimeHairChangeColor(EntityUid uid, MidroundCustomizationComponent component, MidroundCustomizationChangeColorMessage message)
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

        _humanoid.SetMarkingColor(uid, args.Category, args.Slot, args.Colors, force: false);

        // using this makes the UI feel like total ass
        // que
        // UpdateInterface(uid, component.Target, message.Session);
    }

    private void OnTrySlimeHairRemoveSlot(EntityUid uid, MidroundCustomizationComponent component, MidroundCustomizationRemoveSlotMessage message)
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

        _humanoid.RemoveMarking(uid, args.Category, args.Slot);

        _audio.PlayPvs(component.ChangeMarkingSound, uid);
        UpdateInterface(uid, component);
    }

    private void OnTrySlimeHairAddSlot(EntityUid uid, MidroundCustomizationComponent component, MidroundCustomizationAddSlotMessage message)
    {
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
    }

    private void OnAddSlotDoAfter(EntityUid uid, MidroundCustomizationComponent component, SlimeHairAddSlotDoAfterEvent args)
    {
        if (args.Handled || args.Target == null || args.Cancelled || !TryComp(uid, out HumanoidAppearanceComponent? humanoid))
            return;

        var marking = _markings.MarkingsByCategoryAndSpecies(args.Category, humanoid.Species).Keys.FirstOrDefault();

        if (string.IsNullOrEmpty(marking))
            return;

        _audio.PlayPvs(component.ChangeMarkingSound, uid);
        _humanoid.AddMarking(uid, marking, component.DefaultSkinColoring ? humanoid.SkinColor : Color.Gray);
        UpdateInterface(uid, component);
    }

    private void OnTrySlimeHairChangeVoice(EntityUid uid, MidroundCustomizationComponent component, MidroundCustomizationChangeVoiceMessage message)
    {
        _doAfterSystem.Cancel(component.DoAfter);
        component.DoAfter = null;

        var doAfter = new SlimeHairChangeVoiceDoAfterEvent()
        {
            Voice = message.TTS,
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

        if (!TryComp<HumanoidAppearanceComponent>(args.Target.Value, out var humanoid))
            return;

        if (!_proto.TryIndex<TTSVoicePrototype>(args.Voice, out var proto) || !HumanoidCharacterProfile.CanHaveVoice(proto, humanoid.Sex))
            return;

        _audio.PlayPvs(component.ChangeMarkingSound, uid);
        _humanoid.SetTTSVoice(args.Target.Value, args.Voice, humanoid);

        UpdateInterface(uid, component);
    }

    private void OnTryChangeBarkProto(EntityUid uid, MidroundCustomizationComponent component, MidroundCustomizationChangeBarkProtoMessage message)
    {
        _doAfterSystem.Cancel(component.DoAfter);
        component.DoAfter = null;

        var doAfter = new SlimeHairChangeBarkProtoDoAfterEvent()
        {
            Proto = message.Proto,
        };

        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, uid, component.ChangeVoiceTime, doAfter, uid, target: uid, used: uid)
        {
            DistanceThreshold = SharedInteractionSystem.InteractionRange,
            BreakOnDamage = true,
        }, out var doAfterId);

        component.DoAfter = doAfterId;
    }

    private void OnChangeBarkProtoDoAfter(EntityUid uid, MidroundCustomizationComponent component, SlimeHairChangeBarkProtoDoAfterEvent args)
    {
        if (args.Handled || args.Target == null || args.Cancelled)
            return;

        if (!TryComp<HumanoidAppearanceComponent>(args.Target.Value, out var humanoid))
            return;

        if (!_proto.TryIndex<BarkPrototype>(args.Proto, out _))
            return;

        _audio.PlayPvs(component.ChangeMarkingSound, uid);
        var newData = humanoid.Bark.WithProto(args.Proto);
        _humanoid.SetBarkData(args.Target.Value, newData, humanoid);

        UpdateInterface(uid, component);
    }

    private void OnTryChangeBarkPitch(EntityUid uid, MidroundCustomizationComponent component, MidroundCustomizationChangeBarkPitchMessage message)
    {
        _doAfterSystem.Cancel(component.DoAfter);
        component.DoAfter = null;

        var doAfter = new SlimeHairChangeBarkPitchDoAfterEvent()
        {
            Pitch = message.Pitch,
        };

        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, uid, component.ChangeVoiceTime, doAfter, uid, target: uid, used: uid)
        {
            DistanceThreshold = SharedInteractionSystem.InteractionRange,
            BreakOnDamage = true,
        }, out var doAfterId);

        component.DoAfter = doAfterId;
    }

    private void OnChangeBarkPitchDoAfter(EntityUid uid, MidroundCustomizationComponent component, SlimeHairChangeBarkPitchDoAfterEvent args)
    {
        if (args.Handled || args.Target == null || args.Cancelled)
            return;

        if (!TryComp<HumanoidAppearanceComponent>(args.Target.Value, out var humanoid))
            return;

        _audio.PlayPvs(component.ChangeMarkingSound, uid);
        var newData = humanoid.Bark.WithPitch(args.Pitch);
        _humanoid.SetBarkData(args.Target.Value, newData, humanoid);

        UpdateInterface(uid, component);
    }

    private void OnTryChangeBarkMinVar(EntityUid uid, MidroundCustomizationComponent component, MidroundCustomizationChangeBarkMinVarMessage message)
    {
        _doAfterSystem.Cancel(component.DoAfter);
        component.DoAfter = null;

        var doAfter = new SlimeHairChangeBarkMinVarDoAfterEvent()
        {
            MinVar = message.MinVar,
        };

        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, uid, component.ChangeVoiceTime, doAfter, uid, target: uid, used: uid)
        {
            DistanceThreshold = SharedInteractionSystem.InteractionRange,
            BreakOnDamage = true,
        }, out var doAfterId);

        component.DoAfter = doAfterId;
    }

    private void OnChangeBarkMinVarDoAfter(EntityUid uid, MidroundCustomizationComponent component, SlimeHairChangeBarkMinVarDoAfterEvent args)
    {
        if (args.Handled || args.Target == null || args.Cancelled)
            return;

        if (!TryComp<HumanoidAppearanceComponent>(args.Target.Value, out var humanoid))
            return;

        _audio.PlayPvs(component.ChangeMarkingSound, uid);
        var newData = humanoid.Bark.WithMinVar(args.MinVar);
        _humanoid.SetBarkData(args.Target.Value, newData, humanoid);

        UpdateInterface(uid, component);
    }

    private void OnTryChangeBarkMaxVar(EntityUid uid, MidroundCustomizationComponent component, MidroundCustomizationChangeBarkMaxVarMessage message)
    {
        _doAfterSystem.Cancel(component.DoAfter);
        component.DoAfter = null;

        var doAfter = new SlimeHairChangeBarkMaxVarDoAfterEvent()
        {
            MaxVar = message.MaxVar,
        };

        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, uid, component.ChangeVoiceTime, doAfter, uid, target: uid, used: uid)
        {
            DistanceThreshold = SharedInteractionSystem.InteractionRange,
            BreakOnDamage = true,
        }, out var doAfterId);

        component.DoAfter = doAfterId;
    }

    private void OnChangeBarkMaxVarDoAfter(EntityUid uid, MidroundCustomizationComponent component, SlimeHairChangeBarkMaxVarDoAfterEvent args)
    {
        if (args.Handled || args.Target == null || args.Cancelled)
            return;

        if (!TryComp<HumanoidAppearanceComponent>(args.Target.Value, out var humanoid))
            return;

        _audio.PlayPvs(component.ChangeMarkingSound, uid);
        var newData = humanoid.Bark.WithMaxVar(args.MaxVar);
        _humanoid.SetBarkData(args.Target.Value, newData, humanoid);

        UpdateInterface(uid, component);
    }

    private void UpdateInterface(EntityUid uid, MidroundCustomizationComponent component)
    {
        if (!TryComp<HumanoidAppearanceComponent>(uid, out var humanoid))
            return;

        var markingDict = new Dictionary<MarkingCategories, List<Marking>>();
        var slotsDict = new Dictionary<MarkingCategories, int>();
        foreach (var category in component.CustomizableCategories)
        {
            markingDict[category] = new();
            slotsDict[category] = humanoid.MarkingSet.PointsLeft(category);

            if (humanoid.MarkingSet.TryGetCategory(category, out var markings))
                markingDict[category] = markings.ToList();
        }

        var state = new MidroundCustomizationUiState(
            humanoid.Species,
            humanoid.Sex,
            true,
            humanoid.Voice,
            humanoid.Bark.Proto,
            humanoid.Bark.Pitch,
            humanoid.Bark.MinVar,
            humanoid.Bark.MaxVar,
            markingDict,
            slotsDict);

        _uiSystem.SetUiState(uid, MidroundCustomizationUiKey.Key, state);
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
