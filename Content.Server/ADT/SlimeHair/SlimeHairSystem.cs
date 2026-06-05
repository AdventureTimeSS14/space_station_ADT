using System.Linq;
using Content.Server.DoAfter;
using Content.Shared.Body;
using Content.Shared.UserInterface;
using Content.Shared.DoAfter;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Interaction;
using Content.Shared.ADT.SlimeHair;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
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
    [Dependency] private readonly SharedVisualBodySystem _visualBody = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly ActionsSystem _action = default!;

    private static readonly HashSet<HumanoidVisualLayers> AllowedLayers = new()
    {
        HumanoidVisualLayers.Hair,
        HumanoidVisualLayers.FacialHair,
    };

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SlimeHairComponent, ActivatableUIOpenAttemptEvent>(OnOpenUIAttempt);

        Subs.BuiEvents<SlimeHairComponent>(SlimeHairUiKey.Key, subs =>
        {
            subs.Event<BoundUIClosedEvent>(OnUIClosed);
            subs.Event<SlimeHairSelectMessage>(OnSlimeHairSelect);
        });

        SubscribeLocalEvent<SlimeHairComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<SlimeHairComponent, ComponentShutdown>(OnShutdown);

        SubscribeLocalEvent<SlimeHairComponent, SlimeHairSelectDoAfterEvent>(OnSelectSlotDoAfter);

        InitializeSlimeAbilities();

    }

    private void OnOpenUIAttempt(EntityUid uid, SlimeHairComponent mirror, ActivatableUIOpenAttemptEvent args)
    {
        if (!HasComp<VisualBodyComponent>(uid))
            args.Cancel();
    }

    private void OnSlimeHairSelect(Entity<SlimeHairComponent> ent, ref SlimeHairSelectMessage args)
    {
        if (ent.Comp.Target is not { } target)
            return;

        _doAfterSystem.Cancel(ent.Comp.DoAfter);
        ent.Comp.DoAfter = null;

        var doAfter = new SlimeHairSelectDoAfterEvent()
        {
            Markings = args.Markings,
        };

        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, args.Actor, ent.Comp.SelectSlotTime, doAfter, ent, target: target, used: ent)
        {
            DistanceThreshold = SharedInteractionSystem.InteractionRange,
            BreakOnDamage = true,
            BreakOnMove = true,
            NeedHand = true,
        },
            out var doAfterId);

        ent.Comp.DoAfter = doAfterId;
    }

    private void OnSelectSlotDoAfter(EntityUid uid, SlimeHairComponent component, SlimeHairSelectDoAfterEvent args)
    {
        if (args.Handled || args.Target == null || args.Cancelled)
            return;

        if (component.Target != args.Target)
            return;

        _visualBody.ApplyMarkings(args.Target.Value, args.Markings);
        _audio.PlayPvs(component.ChangeHairSound, uid);
        UpdateInterface(uid, component);
    }

    private void UpdateInterface(EntityUid uid, SlimeHairComponent component)
    {
        if (!_visualBody.TryGatherMarkingsData(uid, AllowedLayers, out var profiles, out var markings, out var applied))
            return;

        var filteredMarkings = FilterMarkingData(markings, AllowedLayers);
        var filteredProfiles = FilterProfiles(profiles, filteredMarkings.Keys);
        var filteredApplied = FilterAppliedMarkings(applied, filteredMarkings.Keys);

        var state = new SlimeHairUiState(filteredProfiles, filteredMarkings, filteredApplied);

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
