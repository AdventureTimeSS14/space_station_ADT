using Content.Server.Chat.Systems;
using Content.Shared.ADT.Xenobiology;
using Content.Shared.ADT.Xenobiology.Components;
using Content.Shared.ADT.Xenobiology.Systems;
using Content.Server.ADT.Xenobiology.Systems;
using Content.Server.NPC;
using Content.Server.NPC.HTN;
using Content.Server.NPC.Systems;
using Content.Shared.Chat;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Systems;
using Content.Shared.Pointing;
using Content.Shared.Speech;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using System.Linq;

namespace Content.Server.ADT.Xenobiology;
public sealed partial class SlimeSpeechSystem : EntitySystem
{
    [Dependency] private readonly SlimeLatchSystem _slimeLatch = default!;
    [Dependency] private readonly NpcFactionSystem _factions = default!;
    [Dependency] private readonly NPCSystem _npc = default!;
    [Dependency] private readonly HTNSystem _htn = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _speedModifier = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private readonly Dictionary<SlimeCommandType, List<string>> _commandKeywords = new();

    private const float FollowUpdateInterval = 0.5f;
    private const float FollowMaxDistance = 25f;
    private const float AttackTargetRange = 30f;
    private const int MaxCommandSpeakers = 3;
    private static readonly TimeSpan PointOrderDuration = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan SayWindowDuration = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Tracks how many slimes already spoke for a given speaker, so a swarm
    /// doesn't spam the chat - only a few of them talk, but all obey.
    /// </summary>
    private readonly Dictionary<EntityUid, (TimeSpan ExpiresAt, int Count)> _sayWindows = new();

    public override void Initialize()
    {
        base.Initialize();

        foreach (var proto in _prototypeManager.EnumeratePrototypes<SlimeCommandPrototype>())
        {
            if (!_commandKeywords.TryGetValue(proto.CommandType, out var keywords))
            {
                keywords = new List<string>();
                _commandKeywords[proto.CommandType] = keywords;
            }

            keywords.AddRange(proto.Keywords);
        }

        SubscribeLocalEvent<SlimeComponent, ListenEvent>(OnSlimeListen);
        SubscribeLocalEvent<SlimeComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshSpeed);
        SubscribeLocalEvent<SlimeAttackOrderComponent, AfterPointedAtEvent>(OnPointedAt);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<SlimeComponent>();
        while (query.MoveNext(out var uid, out var slime))
        {
            UpdateFollowing(uid, slime);
        }

        var stoppedQuery = EntityQueryEnumerator<SlimeStoppedComponent>();
        while (stoppedQuery.MoveNext(out var uid, out var stopped))
        {
            if (_timing.CurTime < stopped.ExpiresAt)
                continue;

            RemCompDeferred<SlimeStoppedComponent>(uid);
            if (TryComp<HTNComponent>(uid, out var htn))
                _htn.SetHTNEnabled((uid, htn), true, 1f);
        }

        var orderQuery = EntityQueryEnumerator<SlimeAttackOrderComponent>();
        while (orderQuery.MoveNext(out var uid, out var order))
        {
            if (_timing.CurTime < order.ExpiresAt)
                continue;

            RemCompDeferred<SlimeAttackOrderComponent>(uid);
        }
    }

    private void OnSlimeListen(Entity<SlimeComponent> slime, ref ListenEvent args)
    {
        if (!IsFriend(slime, args.Source))
            return;

        var message = args.Message.ToLowerInvariant();

        if (!message.Contains(Loc.GetString("slime-speech-address")))
            return;

        if (ContainsCommand(message, SlimeCommandType.Attack) && TryAttackCommand(slime, args.Source, message))
            return;

        if (ContainsCommand(message, SlimeCommandType.Stop) && TryStopCommand(slime, args.Source))
            return;

        if (ContainsCommand(message, SlimeCommandType.Follow) && TryFollowCommand(slime, args.Source))
            return;

        if (ContainsCommand(message, SlimeCommandType.Greet))
            SayForCommand(slime, Loc.GetString("slime-speech-greet"), args.Source);
    }

    private bool ContainsCommand(string message, SlimeCommandType type)
    {
        return _commandKeywords.TryGetValue(type, out var keywords)
            && keywords.Any(message.Contains);
    }

    private bool IsFriend(Entity<SlimeComponent> slime, EntityUid speaker)
    {
        return slime.Comp.Tamer == speaker
            || _factions.IsIgnored(new Entity<FactionExceptionComponent?>(slime, default), speaker);
    }

    private bool TryFollowCommand(Entity<SlimeComponent> slime, EntityUid speaker)
    {
        if (slime.Comp.Friendship < slime.Comp.MinFriendshipToCommand)
            return false;

        CancelAttackOrder(slime, speaker);
        UnfreezeSlime(slime);

        slime.Comp.FollowingTarget = speaker;
        slime.Comp.NextFollowUpdate = TimeSpan.Zero;
        SayForCommand(slime, Loc.GetString("slime-speech-follow", ("target", speaker)), speaker);
        return true;
    }

    private bool TryStopCommand(Entity<SlimeComponent> slime, EntityUid speaker)
    {
        CancelAttackOrder(slime, speaker);

        var hostiles = _factions.GetHostiles(new Entity<FactionExceptionComponent?>(slime, default)).ToList();
        var isFeeding = _slimeLatch.IsLatched(slime) || hostiles.Any(uid => Exists(uid));

        if (isFeeding && slime.Comp.Friendship < slime.Comp.MinFriendshipToCommand)
        {
            slime.Comp.Friendship = Math.Max(0f, slime.Comp.Friendship - slime.Comp.FriendshipLossOnRefusal);
            SayForCommand(slime, Loc.GetString("slime-speech-grrr"), speaker);
            Dirty(slime);
            return true;
        }

        StopFollowing(slime);
        _slimeLatch.Unlatch(slime);

        foreach (var hostile in hostiles)
        {
            _factions.DeAggroEntity(new Entity<FactionExceptionComponent?>(slime, default), hostile);
        }

        if (hostiles.Count > 0)
            RefreshSpeed(slime);

        FreezeSlime(slime, slime.Comp.StopDuration);

        return true;
    }

    private bool TryAttackCommand(Entity<SlimeComponent> slime, EntityUid speaker, string message)
    {
        if (slime.Comp.Friendship < slime.Comp.MinFriendshipToCommand)
            return false;

        var targetName = string.Empty;
        var address = Loc.GetString("slime-speech-address");
        var nameStart = message.IndexOf(address, StringComparison.Ordinal);
        if (nameStart != -1)
        {
            nameStart += address.Length;
            var nameEnd = FindLastCommandKeyword(message, SlimeCommandType.Attack);
            if (nameEnd > nameStart)
                targetName = message[nameStart..nameEnd].Trim(' ', ',', '.', '!', '?');
        }

        var target = targetName.Length > 0 ? FindTargetByName(slime, targetName) : null;

        // No name (or unknown name) - wait for the tamer to point at the target.
        if (target == null)
        {
            var order = EnsureComp<SlimeAttackOrderComponent>(speaker);
            if (!order.Slimes.Contains(slime))
                order.Slimes.Add(slime);
            order.ExpiresAt = _timing.CurTime + PointOrderDuration;
            Dirty(speaker, order);

            // Freeze the slime so it doesn't hunt on its own while waiting.
            FreezeSlime(slime, PointOrderDuration);

            SayForCommand(slime, Loc.GetString("slime-speech-attack-wait"), speaker);
            return true;
        }

        if (IsFriend(slime, target.Value))
        {
            SayForCommand(slime, Loc.GetString("slime-speech-attack-friend"), speaker);
            return true;
        }

        OrderAttack(slime, target.Value, speaker);
        return true;
    }

    private void OnPointedAt(Entity<SlimeAttackOrderComponent> order, ref AfterPointedAtEvent args)
    {
        if (Deleted(args.Pointed))
            return;

        foreach (var slimeUid in order.Comp.Slimes)
        {
            if (!TryComp<SlimeComponent>(slimeUid, out var slimeComp))
                continue;

            var slime = new Entity<SlimeComponent>(slimeUid, slimeComp);

            if (IsFriend(slime, args.Pointed))
            {
                SayForCommand(slime, Loc.GetString("slime-speech-attack-friend"), order.Owner);
                continue;
            }

            OrderAttack(slime, args.Pointed, order.Owner);
        }

        RemCompDeferred<SlimeAttackOrderComponent>(order);
    }

    private void FreezeSlime(Entity<SlimeComponent> slime, TimeSpan duration)
    {
        StopFollowing(slime);
        var stopped = EnsureComp<SlimeStoppedComponent>(slime);
        stopped.ExpiresAt = _timing.CurTime + duration;
        Dirty(slime, stopped);

        if (TryComp<HTNComponent>(slime, out var htn))
            _htn.SetHTNEnabled((slime, htn), false);
    }

    private void UnfreezeSlime(Entity<SlimeComponent> slime)
    {
        RemCompDeferred<SlimeStoppedComponent>(slime);
        if (TryComp<HTNComponent>(slime, out var htn))
            _htn.SetHTNEnabled((slime, htn), true, 0.5f);
    }

    private void OrderAttack(Entity<SlimeComponent> slime, EntityUid target, EntityUid speaker)
    {
        StopFollowing(slime);
        UnfreezeSlime(slime);

        _factions.AggroEntity(new Entity<FactionExceptionComponent?>(slime, default), target);
        RefreshSpeed(slime);
        SayForCommand(slime, Loc.GetString("slime-speech-attack", ("target", target)), speaker);
    }

    private void CancelAttackOrder(Entity<SlimeComponent> slime, EntityUid speaker)
    {
        if (!TryComp<SlimeAttackOrderComponent>(speaker, out var order))
            return;

        order.Slimes.Remove(slime);
        if (order.Slimes.Count == 0)
            RemCompDeferred<SlimeAttackOrderComponent>(speaker);
    }

    private int FindLastCommandKeyword(string message, SlimeCommandType type)
    {
        if (!_commandKeywords.TryGetValue(type, out var keywords))
            return -1;

        var index = -1;
        foreach (var keyword in keywords)
        {
            index = Math.Max(index, message.LastIndexOf(keyword, StringComparison.OrdinalIgnoreCase));
        }

        return index;
    }

    private EntityUid? FindTargetByName(Entity<SlimeComponent> slime, string targetName)
    {
        var coords = Transform(slime).Coordinates;

        foreach (var uid in _lookup.GetEntitiesInRange<MobStateComponent>(coords, AttackTargetRange))
        {
            if (uid.Owner == slime.Owner || _mobState.IsDead(uid))
                continue;

            if (Name(uid).ToLowerInvariant().Contains(targetName))
                return uid.Owner;
        }

        return null;
    }

    private void UpdateFollowing(EntityUid uid, SlimeComponent slime)
    {
        if (slime.FollowingTarget is not { } target)
            return;

        if (Deleted(target)
            || _mobState.IsDead(target)
            || !Transform(target).Coordinates.TryDistance(EntityManager, Transform(uid).Coordinates, out var distance)
            || distance > FollowMaxDistance)
        {
            StopFollowing((uid, slime));
            return;
        }

        if (_timing.CurTime < slime.NextFollowUpdate)
            return;

        slime.NextFollowUpdate = _timing.CurTime + TimeSpan.FromSeconds(FollowUpdateInterval);
        _npc.SetBlackboard(uid, NPCBlackboard.FollowTarget, Transform(target).Coordinates);
    }

    private void StopFollowing(Entity<SlimeComponent> slime)
    {
        if (slime.Comp.FollowingTarget == null)
            return;

        slime.Comp.FollowingTarget = null;
        var htn = CompOrNull<HTNComponent>(slime);
        htn?.Blackboard.Remove<EntityCoordinates>(NPCBlackboard.FollowTarget);
    }

    private void OnRefreshSpeed(Entity<SlimeComponent> slime, ref RefreshMovementSpeedModifiersEvent args)
    {
        var hasHostiles = _factions.GetHostiles(new Entity<FactionExceptionComponent?>(slime, default))
            .Any(uid => Exists(uid));

        if (hasHostiles)
            args.ModifySpeed(slime.Comp.ChaseSpeedMultiplier);
    }

    private void RefreshSpeed(Entity<SlimeComponent> slime)
    {
        _speedModifier.RefreshMovementSpeedModifiers(slime);
    }

    private void Say(EntityUid slime, string message)
    {
        _chat.TrySendInGameICMessage(slime, message, InGameICChatType.Speak, hideChat: false, hideLog: true);
    }

    /// <summary>
    /// Lets at most a few slimes actually speak per command, so a whole swarm
    /// doesn't spam the chat. All of them still obey the order.
    /// </summary>
    private void SayForCommand(EntityUid slime, string message, EntityUid speaker)
    {
        if (_sayWindows.TryGetValue(speaker, out var window))
        {
            if (_timing.CurTime > window.ExpiresAt)
                window = (_timing.CurTime + SayWindowDuration, 0);

            if (window.Count >= MaxCommandSpeakers)
                return;

            _sayWindows[speaker] = (window.ExpiresAt, window.Count + 1);
        }
        else
        {
            _sayWindows[speaker] = (_timing.CurTime + SayWindowDuration, 1);
        }

        Say(slime, message);
    }
}
