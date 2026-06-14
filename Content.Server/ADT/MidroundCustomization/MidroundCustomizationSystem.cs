using System.Linq;
using Content.Server.DoAfter;
using Content.Shared.ADT.MidroundCustomization;
using Content.Shared.Body;
using Content.Shared.UserInterface;
using Content.Shared.DoAfter;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Interaction;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
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

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MidroundCustomizationComponent, ActivatableUIOpenAttemptEvent>(OnOpenUIAttempt);

        Subs.BuiEvents<MidroundCustomizationComponent>(MidroundCustomizationUiKey.Key, subs =>
        {
            subs.Event<BoundUIOpenedEvent>(OnUIOpened);
            subs.Event<BoundUIClosedEvent>(OnUIClosed);
            subs.Event<MidroundCustomizationSelectMessage>(OnMidroundCustomizationSelect);
        });

        SubscribeLocalEvent<MidroundCustomizationComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<MidroundCustomizationComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<MidroundCustomizationComponent, MidroundCustomizationSelectDoAfterEvent>(OnSelectSlotDoAfter);
        
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
        UpdateInterface(uid, component);
    }

    private void UpdateInterface(EntityUid uid, MidroundCustomizationComponent component)
    {
        if (!_visualBody.TryGatherMarkingsData(uid, component.AllowedLayers, out var profiles, out var markings, out var applied))
            return;

        var filteredMarkings = FilterMarkingData(markings, component.AllowedLayers);
        var filteredProfiles = FilterProfiles(profiles, filteredMarkings.Keys);
        var filteredApplied = FilterAppliedMarkings(applied, filteredMarkings.Keys);

        var state = new MidroundCustomizationUiState(filteredProfiles, filteredMarkings, filteredApplied);

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
