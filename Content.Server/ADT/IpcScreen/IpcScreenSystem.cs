using System.Linq;
using Content.Server.DoAfter;
using Content.Shared.Body;
using Content.Shared.UserInterface;
using Content.Shared.DoAfter;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Interaction;
using Content.Shared.ADT.IpcScreen;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Content.Server.Actions;

namespace Content.Server.ADT.IpcScreen;

public sealed partial class IpcScreenSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedVisualBodySystem _visualBody = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly ActionsSystem _action = default!;

    private static readonly HashSet<HumanoidVisualLayers> AllowedLayers = new()
    {
        HumanoidVisualLayers.FacialHair,
    };

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<IpcScreenComponent, ActivatableUIOpenAttemptEvent>(OnOpenUIAttempt);

        Subs.BuiEvents<IpcScreenComponent>(IpcScreenUiKey.Key, subs =>
        {
            subs.Event<BoundUIClosedEvent>(OnUIClosed);
            subs.Event<IpcScreenSelectMessage>(OnIpcScreenSelect);
        });

        SubscribeLocalEvent<IpcScreenComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<IpcScreenComponent, ComponentShutdown>(OnShutdown);

        SubscribeLocalEvent<IpcScreenComponent, IpcScreenSelectDoAfterEvent>(OnSelectSlotDoAfter);

        InitializeIpcScreenAbilities();

    }

    private void OnOpenUIAttempt(EntityUid uid, IpcScreenComponent mirror, ActivatableUIOpenAttemptEvent args)
    {
        if (!HasComp<VisualBodyComponent>(uid))
            args.Cancel();
    }

    private void OnIpcScreenSelect(Entity<IpcScreenComponent> ent, ref IpcScreenSelectMessage args)
    {
        if (ent.Comp.Target is not { } target)
            return;

        _doAfterSystem.Cancel(ent.Comp.DoAfter);
        ent.Comp.DoAfter = null;

        var doAfter = new IpcScreenSelectDoAfterEvent()
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

    private void OnSelectSlotDoAfter(EntityUid uid, IpcScreenComponent component, IpcScreenSelectDoAfterEvent args)
    {
        if (args.Handled || args.Target == null || args.Cancelled)
            return;

        if (component.Target != args.Target)
            return;

        _visualBody.ApplyMarkings(args.Target.Value, args.Markings);
        _audio.PlayPvs(component.ChangeHairSound, uid);
        UpdateInterface(uid, component);
    }

    private void UpdateInterface(EntityUid uid, IpcScreenComponent component)
    {
        if (!_visualBody.TryGatherMarkingsData(uid, AllowedLayers, out var profiles, out var markings, out var applied))
            return;

        var filteredMarkings = FilterMarkingData(markings, AllowedLayers);
        var filteredProfiles = FilterProfiles(profiles, filteredMarkings.Keys);
        var filteredApplied = FilterAppliedMarkings(applied, filteredMarkings.Keys);

        var state = new IpcScreenUiState(filteredProfiles, filteredMarkings, filteredApplied);

        component.Target = uid;
        _uiSystem.SetUiState(uid, IpcScreenUiKey.Key, state);
    }

    private void OnUIClosed(Entity<IpcScreenComponent> ent, ref BoundUIClosedEvent args)
    {
        ent.Comp.Target = null;
    }

    private void OnMapInit(EntityUid uid, IpcScreenComponent component, MapInitEvent args)
    {
        _action.AddAction(uid, ref component.ActionEntity, component.Action);
    }
    private void OnShutdown(EntityUid uid, IpcScreenComponent component, ComponentShutdown args)
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
