using System.Linq;
using Content.Server.DoAfter;
using Content.Shared.ADT.MidroundCustomization;
using Content.Shared.ADT.SpeechBarks;
using Content.Shared.ADT.TTS;
using Content.Shared.Body;
using Content.Shared.UserInterface;
using Content.Shared.DoAfter;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Interaction;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Preferences;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Content.Server.Actions;
using Robust.Shared.Player;

namespace Content.Server.ADT.MidroundCustomization;

public sealed class MidroundCustomizationSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedVisualBodySystem _visualBody = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly ActionsSystem _action = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedPointLightSystem _pointLight = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MidroundCustomizationComponent, ActivatableUIOpenAttemptEvent>(OnOpenUIAttempt);

        Subs.BuiEvents<MidroundCustomizationComponent>(MidroundCustomizationUiKey.Key, subs =>
        {
            subs.Event<BoundUIOpenedEvent>(OnUIOpened);
            subs.Event<BoundUIClosedEvent>(OnUIClosed);
            subs.Event<MidroundCustomizationSelectMessage>(OnMidroundCustomizationSelect);
            subs.Event<MidroundCustomizationChangeVoiceMessage>(OnChangeVoice);
            subs.Event<MidroundCustomizationChangeBarkMessage>(OnChangeBark);
            subs.Event<MidroundCustomizationPointLightColorToggleMessage>(OnPointLightColorToggle);
        });

        SubscribeLocalEvent<MidroundCustomizationComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<MidroundCustomizationComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<MidroundCustomizationComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<MidroundCustomizationComponent, MidroundCustomizationSelectDoAfterEvent>(OnSelectSlotDoAfter);
        SubscribeLocalEvent<MidroundCustomizationComponent, MidroundCustomizationChangeVoiceDoAfterEvent>(OnChangeVoiceDoAfter);
        SubscribeLocalEvent<MidroundCustomizationComponent, MidroundCustomizationChangeBarkDoAfterEvent>(OnChangeBarkDoAfter);

        SubscribeLocalEvent<MidroundCustomizationComponent, MidroundCustomizationActionEvent>(OnMidroundCustomizationAction);
    }

    private void OnOpenUIAttempt(EntityUid uid, MidroundCustomizationComponent mirror, ActivatableUIOpenAttemptEvent args)
    {
        if (!HasComp<VisualBodyComponent>(uid))
            args.Cancel();
    }

    private void OnMidroundCustomizationAction(EntityUid uid, MidroundCustomizationComponent comp, MidroundCustomizationActionEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<ActorComponent>(uid, out var actor))
            return;

        _uiSystem.TryOpenUi(uid, MidroundCustomizationUiKey.Key, actor.Owner);
        UpdateInterface(uid, comp);
        args.Handled = true;
    }

    private void OnMidroundCustomizationSelect(Entity<MidroundCustomizationComponent> ent, ref MidroundCustomizationSelectMessage args)
    {
        if (ent.Comp.Target is not { } target)
            return;

        _doAfterSystem.Cancel(ent.Comp.DoAfter);
        ent.Comp.DoAfter = null;

        var doAfter = new MidroundCustomizationSelectDoAfterEvent()
        {
            Markings = args.Markings,
        };

        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, args.Actor, ent.Comp.SelectSlotTime, doAfter, ent, target: target, used: ent)
        {
            DistanceThreshold = SharedInteractionSystem.InteractionRange,
            BreakOnDamage = true,
            BreakOnMove = false,
            NeedHand = true,
        },
            out ent.Comp.DoAfter);
    }

    private void OnSelectSlotDoAfter(EntityUid uid, MidroundCustomizationComponent component, MidroundCustomizationSelectDoAfterEvent args)
    {
        if (args.Handled || args.Target == null || args.Cancelled)
            return;

        if (component.Target != args.Target)
            return;

        _visualBody.ApplyMarkings(args.Target.Value, args.Markings);
        _audio.PlayPvs(component.ChangeHairSound, uid);
        UpdatePointLightColor(uid, component);
        UpdateInterface(uid, component);
    }

    private void OnChangeVoice(Entity<MidroundCustomizationComponent> ent, ref MidroundCustomizationChangeVoiceMessage args)
    {
        if (!HasComp<TTSComponent>(ent))
            return;

        var doAfter = new MidroundCustomizationChangeVoiceDoAfterEvent()
        {
            Voice = args.Voice,
        };

        StartVoiceDoAfter(ent, doAfter);
    }

    private void OnChangeVoiceDoAfter(EntityUid uid, MidroundCustomizationComponent component, MidroundCustomizationChangeVoiceDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        if (!TryComp<TTSComponent>(uid, out var tts) || !TryComp<HumanoidProfileComponent>(uid, out var humanoid))
            return;

        if (!_proto.TryIndex<TTSVoicePrototype>(args.Voice, out var proto))
            return;

        if (!HumanoidCharacterProfile.CanHaveVoice(proto, humanoid.Sex, humanoid.Species))
            return;

        tts.VoicePrototypeId = args.Voice;

        PlayVoiceChangeSound(uid, component);
        UpdateInterface(uid, component);
    }

    private void OnChangeBark(Entity<MidroundCustomizationComponent> ent, ref MidroundCustomizationChangeBarkMessage args)
    {
        if (!HasComp<SpeechBarksComponent>(ent))
            return;

        var doAfter = new MidroundCustomizationChangeBarkDoAfterEvent()
        {
            Proto = args.Proto,
            Pitch = args.Pitch,
            MinVar = args.MinVar,
            MaxVar = args.MaxVar,
        };

        StartVoiceDoAfter(ent, doAfter);
    }

    private void OnChangeBarkDoAfter(EntityUid uid, MidroundCustomizationComponent component, MidroundCustomizationChangeBarkDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        if (!TryComp<SpeechBarksComponent>(uid, out var barks))
            return;

        if (!_proto.TryIndex<BarkPrototype>(args.Proto, out var barkProto))
            return;

        var data = barks.Data.WithProto(args.Proto);
        data.Pitch = args.Pitch;
        data.MinVar = args.MinVar;
        data.MaxVar = args.MaxVar;
        data.Sound = barkProto.Sound;
        barks.Data = data;

        PlayVoiceChangeSound(uid, component);
        UpdateInterface(uid, component);
    }

    private void StartVoiceDoAfter(Entity<MidroundCustomizationComponent> ent, DoAfterEvent doAfter)
    {
        _doAfterSystem.Cancel(ent.Comp.DoAfter);
        ent.Comp.DoAfter = null;

        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, ent, ent.Comp.ChangeVoiceTime, doAfter, ent, target: ent, used: ent)
        {
            DistanceThreshold = SharedInteractionSystem.InteractionRange,
            BreakOnDamage = true,
            BreakOnMove = false,
        },
            out ent.Comp.DoAfter);
    }

    private void PlayVoiceChangeSound(EntityUid uid, MidroundCustomizationComponent component)
    {
        if (!component.PlaySoundForVoiceChange)
            return;

        _audio.PlayPvs(component.ChangeHairSound, uid);
    }

    private void OnPointLightColorToggle(Entity<MidroundCustomizationComponent> ent, ref MidroundCustomizationPointLightColorToggleMessage args)
    {
        if (!ent.Comp.PointLightColor || ent.Comp.PointLightColorEnabled == args.Enabled)
            return;

        if (args.Enabled)
        {
            if (_pointLight.TryGetLight(ent, out var light))
                ent.Comp.OriginalPointLightColor = light.Color;

            ent.Comp.PointLightColorEnabled = true;
            UpdatePointLightColor(ent, ent.Comp);
        }
        else
        {
            _pointLight.SetColor(ent, ent.Comp.OriginalPointLightColor);
            ent.Comp.PointLightColorEnabled = false;
        }

        Dirty(ent);
        UpdateInterface(ent, ent.Comp);
    }

    private void UpdatePointLightColor(EntityUid uid, MidroundCustomizationComponent component)
    {
        if (!component.PointLightColorEnabled)
            return;

        if (!TryGetLayerColor(uid, component.PointLightLayer, out var color))
        {
            _pointLight.SetColor(uid, component.OriginalPointLightColor);
            return;
        }

        _pointLight.SetColor(uid, color);
    }

    private bool TryGetLayerColor(EntityUid uid, HumanoidVisualLayers layer, out Color color)
    {
        color = default;

        if (!_visualBody.TryGatherMarkingsData(uid, new HashSet<HumanoidVisualLayers> { layer }, out _, out _, out var applied))
            return false;

        foreach (var (_, layers) in applied)
        {
            if (!layers.TryGetValue(layer, out var markings) || markings.Count == 0)
                continue;

            if (markings[0].MarkingColors.Count == 0)
                continue;

            color = markings[0].MarkingColors[0];
            return true;
        }

        return false;
    }

    private void OnMobStateChanged(Entity<MidroundCustomizationComponent> ent, ref MobStateChangedEvent args)
    {
        if (ent.Comp.ChangeSlotOnState.Count == 0)
            return;

        var oldState = args.OldMobState;
        var newState = args.NewMobState;

        var wasManaged = ent.Comp.ChangeSlotOnState.Any(entry => entry.State == oldState);
        var isManaged = ent.Comp.ChangeSlotOnState.Any(entry => entry.State == newState);

        if (isManaged)
        {
            if (!wasManaged)
                RecordOriginalMarkings(ent, ent.Comp);

            ApplyStateMarkings(ent, ent.Comp, newState);
            return;
        }

        if (wasManaged)
            RestoreOriginalMarkings(ent, ent.Comp);
    }

    private void RecordOriginalMarkings(EntityUid uid, MidroundCustomizationComponent component)
    {
        component.OriginalMarkings.Clear();

        if (!_visualBody.TryGatherMarkingsData(uid, null, out _, out _, out var applied))
            return;

        foreach (var entry in component.ChangeSlotOnState)
        {
            if (!applied.TryGetValue(entry.Organ, out var layers) || !layers.TryGetValue(entry.Layer, out var markings))
                continue;

            component.OriginalMarkings[(entry.Organ, entry.Layer)] = markings.Select(marking => new Marking(marking.MarkingId, marking.MarkingColors.ToList())).ToList();
        }
    }

    private void ApplyStateMarkings(EntityUid uid, MidroundCustomizationComponent component, MobState state)
    {
        var toApply = new Dictionary<ProtoId<OrganCategoryPrototype>, Dictionary<HumanoidVisualLayers, List<Marking>>>();

        foreach (var entry in component.ChangeSlotOnState)
        {
            if (entry.State != state)
                continue;

            if (!_proto.HasIndex<MarkingPrototype>(entry.Marking))
            {
                Log.Warning($"MidroundCustomization: маркировки {entry.Marking} не существует, слот не подменён.");
                continue;
            }

            if (!toApply.TryGetValue(entry.Organ, out var layers))
            {
                layers = new Dictionary<HumanoidVisualLayers, List<Marking>>();
                toApply[entry.Organ] = layers;
            }

            if (!layers.TryGetValue(entry.Layer, out var markings))
            {
                markings = BuildLayerMarkings(component, entry);
                layers[entry.Layer] = markings;
            }

            var colors = new List<Color> { Color.White };
            if (entry.Colors.Count > 0)
                colors = entry.Colors.ToList();

            var replacement = new Marking(entry.Marking, colors);

            if (entry.Slot < markings.Count)
                markings[entry.Slot] = replacement;
            else
                markings.Add(replacement);
        }

        if (toApply.Count == 0)
            return;

        _visualBody.ApplyMarkings(uid, toApply);
        UpdatePointLightColor(uid, component);
    }

    private static List<Marking> BuildLayerMarkings(MidroundCustomizationComponent component, ChangeSlotOnStateEntry entry)
    {
        if (!component.OriginalMarkings.TryGetValue((entry.Organ, entry.Layer), out var original))
            return new List<Marking>();

        return original.Select(marking => new Marking(marking.MarkingId, marking.MarkingColors.ToList())).ToList();
    }

    private void RestoreOriginalMarkings(EntityUid uid, MidroundCustomizationComponent component)
    {
        if (component.OriginalMarkings.Count == 0)
            return;

        var toApply = new Dictionary<ProtoId<OrganCategoryPrototype>, Dictionary<HumanoidVisualLayers, List<Marking>>>();

        foreach (var ((organ, layer), markings) in component.OriginalMarkings)
        {
            if (!toApply.TryGetValue(organ, out var layers))
            {
                layers = new Dictionary<HumanoidVisualLayers, List<Marking>>();
                toApply[organ] = layers;
            }

            layers[layer] = markings.Select(marking => new Marking(marking.MarkingId, marking.MarkingColors.ToList())).ToList();
        }

        component.OriginalMarkings.Clear();

        _visualBody.ApplyMarkings(uid, toApply);
        UpdatePointLightColor(uid, component);
    }

    private void UpdateInterface(EntityUid uid, MidroundCustomizationComponent component)
    {
        if (!_visualBody.TryGatherMarkingsData(uid, component.AllowedLayers, out var profiles, out var markings, out var applied))
            return;

        var filteredMarkings = FilterMarkingData(markings, component.AllowedLayers);
        var filteredProfiles = FilterProfiles(profiles, filteredMarkings.Keys);
        var filteredApplied = FilterAppliedMarkings(applied, filteredMarkings.Keys);

        var state = new MidroundCustomizationUiState(filteredProfiles, filteredMarkings, filteredApplied)
        {
            PointLightColor = component.PointLightColor,
            PointLightColorEnabled = component.PointLightColorEnabled,
        };

        if (TryComp<HumanoidProfileComponent>(uid, out var humanoid))
        {
            state.Species = humanoid.Species;
            state.Sex = humanoid.Sex;
        }

        if (TryComp<TTSComponent>(uid, out var tts))
            state.Voice = tts.VoicePrototypeId?.Id;

        if (TryComp<SpeechBarksComponent>(uid, out var barks))
        {
            state.BarkProto = barks.Data.Proto;
            state.BarkPitch = barks.Data.Pitch;
            state.BarkMinVar = barks.Data.MinVar;
            state.BarkMaxVar = barks.Data.MaxVar;
        }

        component.Target = uid;
        _uiSystem.SetUiState(uid, MidroundCustomizationUiKey.Key, state);
    }

    private void OnUIOpened(Entity<MidroundCustomizationComponent> ent, ref BoundUIOpenedEvent args)
    {
        UpdateInterface(ent.Owner, ent.Comp);
    }

    private void OnUIClosed(Entity<MidroundCustomizationComponent> ent, ref BoundUIClosedEvent args)
    {
        ent.Comp.Target = null;
    }

    private void OnMapInit(EntityUid uid, MidroundCustomizationComponent component, MapInitEvent args)
    {
        _action.AddAction(uid, ref component.ActionEntity, component.Action);

        if (component.ChangeSlotOnState.Count == 0)
            return;

        if (!TryComp<MobStateComponent>(uid, out var mobState))
            return;

        if (!component.ChangeSlotOnState.Any(entry => entry.State == mobState.CurrentState))
            return;

        RecordOriginalMarkings(uid, component);
        ApplyStateMarkings(uid, component, mobState.CurrentState);
    }

    private void OnShutdown(EntityUid uid, MidroundCustomizationComponent component, ComponentShutdown args)
    {
        _action.RemoveAction(uid, component.ActionEntity);
    }

    private static Dictionary<ProtoId<OrganCategoryPrototype>, OrganMarkingData> FilterMarkingData(
        Dictionary<ProtoId<OrganCategoryPrototype>, OrganMarkingData> markings,
        HashSet<HumanoidVisualLayers> allowedLayers)
    {
        var filtered = new Dictionary<ProtoId<OrganCategoryPrototype>, OrganMarkingData>();

        foreach (var (organ, data) in markings)
        {
            var layers = data.Layers.Where(allowedLayers.Contains).ToHashSet();
            if (layers.Count == 0)
                continue;

            filtered[organ] = new OrganMarkingData
            {
                Group = data.Group,
                Layers = layers,
            };
        }

        return filtered;
    }

    private static Dictionary<ProtoId<OrganCategoryPrototype>, OrganProfileData> FilterProfiles(
        Dictionary<ProtoId<OrganCategoryPrototype>, OrganProfileData> profiles,
        IEnumerable<ProtoId<OrganCategoryPrototype>> organs)
    {
        var organSet = organs.ToHashSet();
        return profiles
            .Where(pair => organSet.Contains(pair.Key))
            .ToDictionary();
    }

    private static Dictionary<ProtoId<OrganCategoryPrototype>, Dictionary<HumanoidVisualLayers, List<Marking>>> FilterAppliedMarkings(
        Dictionary<ProtoId<OrganCategoryPrototype>, Dictionary<HumanoidVisualLayers, List<Marking>>> applied,
        IEnumerable<ProtoId<OrganCategoryPrototype>> organs)
    {
        var organSet = organs.ToHashSet();
        return applied
            .Where(pair => organSet.Contains(pair.Key))
            .ToDictionary(pair => pair.Key, pair => pair.Value);
    }
}
