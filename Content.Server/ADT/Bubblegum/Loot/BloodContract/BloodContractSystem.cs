using Content.Server.Chat.Systems;
using Content.Server.Objectives.Components;
using Content.Server.Objectives.Systems;
using Content.Shared.ADT.Bubblegum.Loot;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Objectives.Components;
using Content.Shared.Objectives.Systems;
using Content.Shared.Popups;
using Content.Shared.Station;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Bubblegum.Loot;

public sealed class BloodContractSystem : EntitySystem
{
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedObjectivesSystem _objectives = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedStationSystem _station = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly TargetObjectiveSystem _target = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BloodContractComponent, BoundUIOpenedEvent>(OnUiOpened);
        SubscribeLocalEvent<BloodContractComponent, BloodContractSelectMessage>(OnSelect);
        SubscribeLocalEvent<BloodContractComponent, BoundUIClosedEvent>(OnUiClosed);
        SubscribeLocalEvent<BloodContractVictimComponent, MobStateChangedEvent>(OnVictimStateChanged);
    }

    private void OnUiOpened(Entity<BloodContractComponent> ent, ref BoundUIOpenedEvent args)
    {
        var targets = new List<BloodContractTargetInfo>();
        ent.Comp.ValidTargets.Clear();

        var query = EntityQueryEnumerator<ActorComponent, MobStateComponent, MetaDataComponent>();
        while (query.MoveNext(out var uid, out _, out _, out var meta))
        {
            if (uid == args.Actor)
                continue;

            if (!_mobState.IsAlive(uid))
                continue;

            if (_station.GetOwningStation(uid) == null)
                continue;

            var netEntity = GetNetEntity(uid);
            ent.Comp.ValidTargets.Add(netEntity);

            targets.Add(new BloodContractTargetInfo
            {
                Entity = netEntity,
                Name = meta.EntityName,
            });
        }

        _ui.SetUiState(ent.Owner, BloodContractUiKey.Key, new BloodContractBuiState(targets));
    }

    private void OnSelect(Entity<BloodContractComponent> ent, ref BloodContractSelectMessage args)
    {
        if (ent.Comp.Used)
            return;

        if (!ent.Comp.ValidTargets.Contains(args.Target))
            return;

        var victim = GetEntity(args.Target);
        if (!Exists(victim) || victim == args.Actor)
            return;

        if (!HasComp<MobStateComponent>(victim) || !_mobState.IsAlive(victim))
            return;

        ent.Comp.Used = true;

        ent.Comp.ValidTargets.Clear();

        _ui.CloseUi(ent.Owner, BloodContractUiKey.Key);

        var pending = EnsureComp<BloodContractPendingComponent>(victim);
        pending.ApplyAt = _timing.CurTime + ent.Comp.EffectDelay;
        pending.HunterWeapon = ent.Comp.HunterWeapon;
        pending.HunterObjective = ent.Comp.HunterObjective;

        AnnounceFrenzy();

        QueueDel(ent.Owner);
    }

    private void OnUiClosed(Entity<BloodContractComponent> ent, ref BoundUIClosedEvent args)
    {
        ent.Comp.ValidTargets.Clear();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<BloodContractPendingComponent>();
        while (query.MoveNext(out var uid, out var pending))
        {
            if (now < pending.ApplyAt)
                continue;

            ApplyContract((uid, pending));
        }
    }

    private void ApplyContract(Entity<BloodContractPendingComponent> ent)
    {
        var victim = ent.Owner;
        RemCompDeferred<BloodContractPendingComponent>(victim);

        if (!Exists(victim) || !_mobState.IsAlive(victim))
            return;

        EnsureComp<BloodFrenzyComponent>(victim);
        EnsureComp<BloodContractVictimComponent>(victim);
        _popup.PopupEntity(Loc.GetString("adt-blood-contract-marked"), victim, victim, PopupType.LargeCaution);

        EntityUid? victimMind = _mind.TryGetMind(victim, out var mindId, out _) ? mindId : null;

        var query = EntityQueryEnumerator<ActorComponent, MobStateComponent>();
        while (query.MoveNext(out var hunter, out _, out _))
        {
            if (hunter == victim || !_mobState.IsAlive(hunter))
                continue;

            GiveHunter(ent.Comp, hunter, victim, victimMind);
        }
    }

    private void GiveHunter(BloodContractPendingComponent contract, EntityUid hunter, EntityUid victim, EntityUid? victimMind)
    {
        var weapon = Spawn(contract.HunterWeapon, _transform.GetMoverCoordinates(hunter));
        EnsureComp<BloodContractCleaverComponent>(weapon).Victim = victim;

        if (!_hands.TryForcePickupAnyHand(hunter, weapon))
            _transform.SetCoordinates(weapon, _transform.GetMoverCoordinates(hunter));

        if (contract.HunterObjective != null && victimMind != null)
            AddKillObjective(hunter, victimMind.Value, contract.HunterObjective.Value);

        _popup.PopupEntity(Loc.GetString("adt-blood-contract-hunt", ("target", victim)), hunter, hunter, PopupType.LargeCaution);
    }

    private void AddKillObjective(EntityUid hunter, EntityUid victimMind, string objectiveProto)
    {
        if (!_mind.TryGetMind(hunter, out var mindId, out var mind))
            return;

        var objective = _objectives.TryCreateObjective(mindId, mind, objectiveProto);
        if (objective == null)
            return;

        _target.SetTarget(objective.Value, victimMind);

        if (TryComp<ObjectiveComponent>(objective.Value, out var objComp))
        {
            var ev = new ObjectiveAfterAssignEvent(mindId, mind, objComp, MetaData(objective.Value));
            RaiseLocalEvent(objective.Value, ref ev);
        }

        _mind.AddObjective(mindId, mind, objective.Value);
    }

    private void OnVictimStateChanged(Entity<BloodContractVictimComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead)
            return;

        RemCompDeferred<BloodContractVictimComponent>(ent);

        var cleaverQuery = EntityQueryEnumerator<BloodContractCleaverComponent>();
        while (cleaverQuery.MoveNext(out var cleaver, out var comp))
        {
            if (comp.Victim == ent.Owner)
                QueueDel(cleaver);
        }

        if (_mind.TryGetMind(ent.Owner, out var victimMind, out _))
            RemoveHuntObjectives(victimMind);
    }

    private void RemoveHuntObjectives(EntityUid victimMind)
    {
        var query = EntityQueryEnumerator<MindComponent>();
        while (query.MoveNext(out var mindId, out var mind))
        {
            for (var i = mind.Objectives.Count - 1; i >= 0; i--)
            {
                if (IsHuntObjective(mind.Objectives[i], victimMind))
                    _mind.TryRemoveObjective(mindId, mind, i);
            }
        }
    }

    private bool IsHuntObjective(EntityUid objective, EntityUid victimMind)
    {
        if (!TryComp<TargetObjectiveComponent>(objective, out var target) || target.Target != victimMind)
            return false;

        var proto = MetaData(objective).EntityPrototype;
        return proto != null && proto.ID == "ADTBloodContractKillObjective"; // TODO rm shitass hardcode
    }

    private void AnnounceFrenzy()
    {
        _chat.DispatchGlobalAnnouncement(
            Loc.GetString("adt-blood-frenzy-announcement"),
            Loc.GetString("adt-blood-frenzy-announcer"),
            true,
            new SoundPathSpecifier("/Audio/ADT/Phantom/Sounds/freedom-deathmatch.ogg"),
            Color.FromHex("#FF3030"));
    }
}
