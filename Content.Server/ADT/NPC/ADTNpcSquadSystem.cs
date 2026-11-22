using System.Numerics;
using Content.Server.Chat.Systems;
using Content.Server.NPC.HTN;
using Content.Shared.Actions;
using Content.Shared.ADT.NPC;
using Content.Shared.ADT.NPC.Components;
using Content.Shared.Chat;
using Content.Shared.Inventory;
using Content.Shared.Mobs;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Systems;
using Content.Shared.Random.Helpers;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.ADT.NPC;

public sealed partial class ADTNpcSquadSystem : EntitySystem
{
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private HTNSystem _htn = default!;
    [Dependency] private NpcFactionSystem _faction = default!;
    [Dependency] private ChatSystem _chat = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    public const string FollowKey = "ADTSquadFollow";
    public const string PassiveKey = "ADTSquadPassive";
    public const string LeaderCoordinatesKey = "ADTSquadLeaderCoordinates";
    public const string HomeKey = "ADTPatrolHome";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ADTNpcSquadCommanderComponent, MapInitEvent>(OnCommanderMapInit);
        SubscribeLocalEvent<ADTNpcSquadCommanderComponent, ADTSquadOrderActionEvent>(OnOrder);
        SubscribeLocalEvent<ADTNpcSquadCommanderComponent, MobStateChangedEvent>(OnCommanderMobState);
        SubscribeLocalEvent<ADTNpcSquadCommanderComponent, ComponentShutdown>(OnCommanderShutdown);
        SubscribeLocalEvent<ADTNpcCombatMemoryComponent, MapInitEvent>(OnMemberMapInit);
    }

    private void OnMemberMapInit(Entity<ADTNpcCombatMemoryComponent> ent, ref MapInitEvent args)
    {
        if (TryComp<HTNComponent>(ent, out var htn))
            htn.Blackboard.SetValue(HomeKey, _transform.GetMoverCoordinates(ent));
    }

    private void OnCommanderMapInit(Entity<ADTNpcSquadCommanderComponent> ent, ref MapInitEvent args)
    {
        foreach (var (order, proto) in ent.Comp.Actions)
        {
            ent.Comp.ActionEntities[order] = _actions.AddAction(ent, proto);
        }

        UpdateActions(ent);
    }

    private void OnOrder(Entity<ADTNpcSquadCommanderComponent> ent, ref ADTSquadOrderActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        switch (args.Order)
        {
            case ADTSquadOrder.Follow:
                ent.Comp.Follow = true;
                break;
            case ADTSquadOrder.Positions:
                ent.Comp.Follow = false;
                break;
            case ADTSquadOrder.Engage:
                ent.Comp.Passive = false;
                break;
            case ADTSquadOrder.StandDown:
                ent.Comp.Passive = true;
                break;
        }

        Announce(ent, args.Order);
        ApplyOrders(ent);
        UpdateActions(ent);
    }

    private void OnCommanderMobState(Entity<ADTNpcSquadCommanderComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead)
            return;

        ent.Comp.Follow = false;
        UpdateActions(ent);
        ReleaseFollowers(ent);
    }

    private void OnCommanderShutdown(Entity<ADTNpcSquadCommanderComponent> ent, ref ComponentShutdown args)
    {
        ReleaseFollowers(ent);
    }

    private void ReleaseFollowers(Entity<ADTNpcSquadCommanderComponent> ent)
    {
        var query = EntityQueryEnumerator<HTNComponent>();

        while (query.MoveNext(out var uid, out var htn))
        {
            if (uid == ent.Owner || TerminatingOrDeleted(uid))
                continue;

            if (!htn.Blackboard.TryGetValue<EntityCoordinates>(LeaderCoordinatesKey, out var leader, EntityManager) ||
                leader.EntityId != ent.Owner)
            {
                continue;
            }

            htn.Blackboard.SetValue(FollowKey, false);
            htn.Blackboard.Remove<EntityCoordinates>(LeaderCoordinatesKey);
            Replan(htn);
        }
    }

    private void Replan(HTNComponent htn)
    {
        if (htn.Plan != null)
        {
            _htn.ShutdownTask(htn.Plan.CurrentOperator, htn.Blackboard, HTNOperatorStatus.Failed);
            _htn.ShutdownPlan(htn);
        }

        _htn.Replan(htn);
    }

    private void Announce(Entity<ADTNpcSquadCommanderComponent> ent, ADTSquadOrder order)
    {
        if (!ent.Comp.Lines.TryGetValue(order, out var lines) || !_proto.TryIndex(lines, out var set))
            return;

        var message = Loc.GetString(_random.Pick(set));
        var radio = false;

        if (ent.Comp.Channel is { } channel &&
            _inventory.TryGetSlotEntity(ent, "ears", out _) &&
            _proto.TryIndex(channel, out var channelProto))
        {
            message = $"{SharedChatSystem.RadioChannelPrefix}{channelProto.KeyCode} {message}";
            radio = true;
        }

        _chat.TrySendInGameICMessage(ent, message, InGameICChatType.Speak, hideChat: false, checkRadioPrefix: radio);
    }

    private void ApplyOrders(Entity<ADTNpcSquadCommanderComponent> ent)
    {
        var map = Transform(ent).MapID;
        var leader = new EntityCoordinates(ent, Vector2.Zero);
        var query = EntityQueryEnumerator<HTNComponent, NpcFactionMemberComponent, TransformComponent>();

        while (query.MoveNext(out var uid, out var htn, out var member, out var xform))
        {
            if (uid == ent.Owner || xform.MapID != map || HasComp<ADTNpcSquadCommanderComponent>(uid))
                continue;

            if (!_faction.IsMember((uid, member), ent.Comp.Faction.Id))
                continue;

            htn.Blackboard.SetValue(FollowKey, ent.Comp.Follow);
            htn.Blackboard.SetValue(PassiveKey, ent.Comp.Passive);
            htn.Blackboard.SetValue(LeaderCoordinatesKey, leader);
            Replan(htn);
        }
    }

    private void UpdateActions(Entity<ADTNpcSquadCommanderComponent> ent)
    {
        SetToggled(ent, ADTSquadOrder.Follow, ent.Comp.Follow);
        SetToggled(ent, ADTSquadOrder.Positions, !ent.Comp.Follow);
        SetToggled(ent, ADTSquadOrder.Engage, !ent.Comp.Passive);
        SetToggled(ent, ADTSquadOrder.StandDown, ent.Comp.Passive);
    }

    private void SetToggled(Entity<ADTNpcSquadCommanderComponent> ent, ADTSquadOrder order, bool toggled)
    {
        if (ent.Comp.ActionEntities.TryGetValue(order, out var action))
            _actions.SetToggled(action, toggled);
    }
}
