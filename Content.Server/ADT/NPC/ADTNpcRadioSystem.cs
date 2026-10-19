using System.Linq;
using System.Numerics;
using Content.Server.Chat.Systems;
using Content.Shared.Chat;
using Content.Shared.Dataset;
using Content.Shared.Inventory;
using Content.Shared.NPC.Systems;
using Content.Shared.Random.Helpers;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.ADT.NPC;

public sealed class ADTNpcRadioSystem : EntitySystem
{
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly NpcFactionSystem _faction = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;

    public const string RadioPrefix = ":я ";
    private const string AnyCall = "*";

    public readonly record struct RadioCall(EntityUid Caller, MapCoordinates Position, string Type, TimeSpan Time);

    private readonly List<RadioCall> _calls = new();
    private readonly Dictionary<(EntityUid, string), TimeSpan> _nextCall = new();

    [ViewVariables]
    public float AnyCallGap = 5f;

    public bool TryCall(EntityUid caller, string type, ProtoId<LocalizedDatasetPrototype> lines, float cooldown, float groupCooldown, float groupRange)
    {
        var now = _timing.CurTime;
        Prune(now);

        if (!_inventory.TryGetSlotEntity(caller, "ears", out _))
            return false;

        if (_nextCall.TryGetValue((caller, type), out var next) && now < next)
            return false;

        if (_nextCall.TryGetValue((caller, AnyCall), out var nextAny) && now < nextAny)
            return false;

        var pos = _transform.GetMapCoordinates(caller);

        foreach (var call in _calls)
        {
            if (call.Type != type || (now - call.Time).TotalSeconds >= groupCooldown)
                continue;

            if (call.Caller == caller)
                return false;

            if (!Exists(call.Caller) || !_faction.IsEntityFriendly(caller, call.Caller))
                continue;

            if (call.Position.MapId == pos.MapId && Vector2.Distance(call.Position.Position, pos.Position) <= groupRange)
                return false;
        }

        if (!_proto.TryIndex(lines, out var set))
            return false;

        _nextCall[(caller, type)] = now + TimeSpan.FromSeconds(cooldown);
        _nextCall[(caller, AnyCall)] = now + TimeSpan.FromSeconds(AnyCallGap);
        _calls.Add(new RadioCall(caller, pos, type, now));

        _chat.TrySendInGameICMessage(caller, RadioPrefix + Loc.GetString(_random.Pick(set)), InGameICChatType.Speak, hideChat: false);
        return true;
    }

    public RadioCall? FindCall(EntityUid listener, IReadOnlyCollection<string> types, float maxAge, float minDistance, float maxDistance)
    {
        var now = _timing.CurTime;
        var pos = _transform.GetMapCoordinates(listener);
        RadioCall? best = null;
        var bestDist = float.MaxValue;

        foreach (var call in _calls)
        {
            if (call.Caller == listener || !types.Contains(call.Type))
                continue;

            if ((now - call.Time).TotalSeconds > maxAge || call.Position.MapId != pos.MapId)
                continue;

            if (!Exists(call.Caller) || !_faction.IsEntityFriendly(listener, call.Caller))
                continue;

            var dist = Vector2.Distance(call.Position.Position, pos.Position);

            if (dist < minDistance || dist > maxDistance || dist >= bestDist)
                continue;

            best = call;
            bestDist = dist;
        }

        return best;
    }

    private void Prune(TimeSpan now)
    {
        _calls.RemoveAll(c => (now - c.Time).TotalSeconds > 120);

        var expired = new List<(EntityUid, string)>();

        foreach (var (key, time) in _nextCall)
        {
            if (time < now)
                expired.Add(key);
        }

        foreach (var key in expired)
            _nextCall.Remove(key);
    }
}
