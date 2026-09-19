using Content.Shared.ADT.Heretic.Components;
using Content.Shared.ADT.Heretic.Messages;
using Content.Shared.ADT.Heretic.Systems;
using Content.Shared.Body;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Nutrition.Components;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Content.Shared.UserInterface;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Physics.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.ADT.Heretic.Systems;

public sealed partial class MawedCrucibleSystem : EntitySystem
{
    [Dependency] private INetManager _net = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private TagSystem _tag = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedSolutionContainerSystem _sol = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedHereticSystem _heretic = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MawedCrucibleComponent, InteractUsingEvent>(OnInteract);
        SubscribeLocalEvent<MawedCrucibleComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<MawedCrucibleComponent, ActivatableUIOpenAttemptEvent>(OnUiAttempt);
        SubscribeLocalEvent<MawedCrucibleComponent, MawedCrucibleMessage>(OnBrewMessage);
    }

    private void OnUiAttempt(Entity<MawedCrucibleComponent> ent, ref ActivatableUIOpenAttemptEvent args)
    {
        if (!Transform(ent).Anchored || ent.Comp.CurrentMass < ent.Comp.MaxMass)
            args.Cancel();
    }

    private void OnBrewMessage(Entity<MawedCrucibleComponent> ent, ref MawedCrucibleMessage args)
    {
        if (_net.IsClient)
            return;

        if (!_heretic.IsHereticOrGhoul(args.Actor))
            return;

        var proto = args.Proto;
        if (proto == default || !ent.Comp.Potions.Contains(proto))
            return;

        if (ent.Comp.CurrentMass < ent.Comp.MaxMass)
            return;

        if (!Transform(ent).Anchored)
            return;

        Spawn(proto, Transform(ent).Coordinates);
        UpdateMass(ent, ent.Comp.CurrentMass - 1);
        _audio.PlayPvs(ent.Comp.BrewSound, ent);
    }

    private void OnExamine(Entity<MawedCrucibleComponent> ent, ref ExaminedEvent args)
    {
        if (!_heretic.IsHereticOrGhoul(args.Examiner))
            return;

        if (ent.Comp.CurrentMass > 0)
            args.PushMarkup(Loc.GetString("mawed-crucible-examine-can-refill-flask"));

        if (ent.Comp.CurrentMass < ent.Comp.MaxMass)
        {
            args.PushMarkup(Loc.GetString("mawed-crucible-examine-not-full",
                ("to-fill", ent.Comp.MaxMass - ent.Comp.CurrentMass)));
        }
        else
            args.PushMarkup(Loc.GetString("mawed-crucible-examine-full"));

        var isAnchored = Comp<TransformComponent>(ent).Anchored;
        var messageId = isAnchored ? "mawed-crucible-examine-anchored" : "mawed-crucible-examine-unanchored";
        args.PushMarkup(Loc.GetString(messageId));
    }

    private void OnInteract(Entity<MawedCrucibleComponent> ent, ref InteractUsingEvent args)
    {
        if (!_heretic.IsHereticOrGhoul(args.User))
            return;

        var xform = Transform(ent);

        if (_tag.HasTag(args.Used, ent.Comp.AnchorTag))
        {
            ToggleAnchor((ent, xform), ref args);
            return;
        }

        if (!xform.Anchored)
            return;

        if (HasComp<EdibleComponent>(args.Used) && HasComp<OrganComponent>(args.Used))
        {
            RefuelCrucible(ent, ref args);
            return;
        }

        if (!_tag.HasTag(args.Used, ent.Comp.EldritchFlaskTag))
            return;

        RefillFlask(ent, ref args);
    }

    private void RefillFlask(Entity<MawedCrucibleComponent> ent, ref InteractUsingEvent args)
    {
        if (ent.Comp.CurrentMass <= 0)
        {
            _popup.PopupEntity(Loc.GetString("mawed-crucible-not-enough-fuel-message"), ent, args.User);
            return;
        }

        if (!TryComp(args.Used, out SolutionContainerManagerComponent? container))
            return;

        if (!_sol.TryGetSolution((args.Used, container), "drink", out var sol))
            return;

        if (sol.Value.Comp.Solution.AvailableVolume == FixedPoint2.Zero)
        {
            _popup.PopupEntity(Loc.GetString("mawed-crucible-flask-full-message"), ent, args.User);
            return;
        }

        if (!_sol.TryAddReagent(sol.Value, ent.Comp.EldritchEssence, ent.Comp.EldritchEssencePerMass, out _))
            return;

        _audio.PlayPredicted(ent.Comp.BrewSound, ent, args.User);
        UpdateMass(ent, ent.Comp.CurrentMass - 1);
        args.Handled = true;
    }

    private void RefuelCrucible(Entity<MawedCrucibleComponent> ent, ref InteractUsingEvent args)
    {
        if (_timing.ApplyingState)
            return;

        if (ent.Comp.CurrentMass >= ent.Comp.MaxMass)
        {
            _popup.PopupEntity(Loc.GetString("mawed-crucible-full-message"), ent, args.User);
            return;
        }

        args.Handled = true;
        UpdateMass(ent, ent.Comp.CurrentMass + 1);
        _audio.PlayPredicted(ent.Comp.MassGainSound, ent, args.User);
        PredictedQueueDel(args.Used);
    }

    private void ToggleAnchor(Entity<TransformComponent> ent, ref InteractUsingEvent args)
    {
        if (ent.Comp.Anchored)
        {
            _transform.Unanchor(ent);
            _popup.PopupEntity(Loc.GetString("anchorable-unanchored"), ent, args.User);
            args.Handled = true;
            return;
        }

        if (!_transform.AnchorEntity(ent))
        {
            _popup.PopupEntity(Loc.GetString("anchorable-occupied"), ent, args.User);
            return;
        }

        _popup.PopupEntity(Loc.GetString("anchorable-anchored"), ent, args.User);
        args.Handled = true;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_net.IsClient)
            return;

        var query = EntityQueryEnumerator<MawedCrucibleComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var comp, out var xform))
        {
            if (!xform.Anchored || comp.CurrentMass >= comp.MaxMass)
            {
                comp.Accumulator = 0f;
                continue;
            }

            comp.Accumulator += frameTime;

            if (comp.Accumulator < comp.MassGainTime)
                continue;

            comp.Accumulator = 0f;
            UpdateMass((uid, comp), comp.CurrentMass + 1);
            _audio.PlayPvs(comp.MassGainSound, uid);
        }
    }

    private void UpdateMass(Entity<MawedCrucibleComponent> ent, int newMass)
    {
        var empty = newMass == 0;
        _appearance.SetData(ent, CrucibleVisuals.Empty, empty);

        ent.Comp.CurrentMass = newMass;
        Dirty(ent);
    }
}
