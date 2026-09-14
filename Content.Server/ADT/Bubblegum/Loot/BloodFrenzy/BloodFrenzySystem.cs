using System.Linq;
using Content.Shared.ADT.Bubblegum.Loot;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Components;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Objectives.Systems;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Bubblegum.Loot;

public sealed class BloodFrenzySystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedObjectivesSystem _objectives = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BloodFrenzyComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<BloodFrenzyComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<BloodFrenzyComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnMobStateChanged(Entity<BloodFrenzyComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Dead)
            RemCompDeferred<BloodFrenzyComponent>(ent);
    }

    private void OnStartup(Entity<BloodFrenzyComponent> ent, ref ComponentStartup args)
    {
        if (ent.Comp.Started)
            return;

        ent.Comp.Started = true;
        ent.Comp.EndsAt = _timing.CurTime + ent.Comp.Duration;

        _popup.PopupEntity(Loc.GetString("adt-blood-frenzy-start"), ent, ent, PopupType.LargeCaution);
        ent.Comp.MusicStream = _audio.PlayPvs(ent.Comp.StartSound, ent)?.Entity;

        ArmVictim(ent);
        DoseVictim(ent);
        AddObjective(ent);
    }

    private void ArmVictim(Entity<BloodFrenzyComponent> ent)
    {
        if (!HasComp<HandsComponent>(ent))
            return;

        foreach (var held in _hands.EnumerateHeld(ent.Owner).ToArray())
        {
            _hands.TryDrop(ent.Owner, held, checkActionBlocker: false);
        }

        var weapon = Spawn(ent.Comp.WeaponProto, _transform.GetMoverCoordinates(ent.Owner));
        var unremoveable = EnsureComp<UnremoveableComponent>(weapon);
        unremoveable.DeleteOnDrop = true;

        if (!_hands.TryForcePickupAnyHand(ent.Owner, weapon))
            _transform.SetCoordinates(weapon, _transform.GetMoverCoordinates(ent.Owner));

        ent.Comp.SpawnedWeapon = weapon;
    }

    private void DoseVictim(Entity<BloodFrenzyComponent> ent)
    {
        if (ent.Comp.Reagent == null || ent.Comp.ReagentAmount <= 0f)
            return;

        if (!TryComp<BloodstreamComponent>(ent, out var bloodstream))
            return;

        if (!_solutionContainer.EnsureSolutionEntity(ent.Owner, bloodstream.MetabolitesSolutionName, out var metabolites))
            return;

        var solution = new Solution(ent.Comp.Reagent, FixedPoint2.New(ent.Comp.ReagentAmount));
        _solutionContainer.TryAddSolution(metabolites.Value, solution);
    }

    private void AddObjective(Entity<BloodFrenzyComponent> ent)
    {
        if (ent.Comp.Objective == null)
            return;

        if (!_mind.TryGetMind(ent.Owner, out var mindId, out var mind))
            return;

        var objective = _objectives.TryCreateObjective(mindId, mind, ent.Comp.Objective.Value);
        if (objective == null)
            return;

        _mind.AddObjective(mindId, mind, objective.Value);
        ent.Comp.ObjectiveEntity = objective.Value;
    }

    private void OnShutdown(Entity<BloodFrenzyComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.SpawnedWeapon is { } weapon && !TerminatingOrDeleted(weapon))
            QueueDel(weapon);

        if (ent.Comp.MusicStream is { } music)
        {
            _audio.Stop(music);
            ent.Comp.MusicStream = null;
        }

        RemoveObjective(ent);
    }

    private void RemoveObjective(Entity<BloodFrenzyComponent> ent)
    {
        if (ent.Comp.ObjectiveEntity is not { } objective)
            return;

        ent.Comp.ObjectiveEntity = null;

        if (!_mind.TryGetMind(ent.Owner, out var mindId, out var mind))
            return;

        var index = mind.Objectives.IndexOf(objective);
        if (index >= 0)
            _mind.TryRemoveObjective(mindId, mind, index);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;

        var pendingQuery = EntityQueryEnumerator<BloodFrenzyPendingComponent>();
        while (pendingQuery.MoveNext(out var uid, out var pending))
        {
            if (now < pending.ApplyAt)
                continue;

            RemCompDeferred<BloodFrenzyPendingComponent>(uid);
            EnsureComp<BloodFrenzyComponent>(uid);
        }

        var query = EntityQueryEnumerator<BloodFrenzyComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (!comp.Started || now < comp.EndsAt)
                continue;

            _popup.PopupEntity(Loc.GetString("adt-blood-frenzy-end"), uid, uid);
            RemCompDeferred<BloodFrenzyComponent>(uid);
        }
    }
}
