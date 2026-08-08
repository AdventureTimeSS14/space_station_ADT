using System.Linq;
using System.Numerics;
using Content.Server.ADT.Generation;
using Content.Server.ADT.Procedural;
using Content.Server.Chat.Managers;
using Content.Server.Procedural;
using Content.Shared.ADT.Lavaland.Events;
using Content.Shared.Chat;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Lavaland.Events;

public sealed class ADTLavalandEventSystem : ADTSharedLavalandEventSystem
{
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    private const int MaxAttempts = 200;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTLavalandEventSchedulerComponent, MapInitEvent>(OnSchedulerMapInit);
    }

    private void OnSchedulerMapInit(Entity<ADTLavalandEventSchedulerComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextEvent = _timing.CurTime + TimeSpan.FromSeconds(ent.Comp.Delay.Next(_random));
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<ADTLavalandEventSchedulerComponent>();

        while (query.MoveNext(out var map, out var comp))
        {
            if (now < comp.NextEvent)
                continue;

            comp.NextEvent = now + TimeSpan.FromSeconds(comp.Delay.Next(_random));

            if (TryPickEvent(comp.Events, out var picked))
                RunEvent(map, picked);
        }
    }

    public override void RunEvent(EntityUid map, ProtoId<ADTLavalandEventPrototype> id)
    {
        if (!_prototype.TryIndex(id, out var proto))
        {
            Log.Error($"Tried to run unknown lavaland event {id}.");
            return;
        }

        RunEvent(map, proto);
    }

    public void RunEvent(EntityUid map, ADTLavalandEventPrototype proto)
    {
        Announce(map, proto);

        var args = new ADTLavalandEventArgs(map, this, _random);

        foreach (var effect in proto.Effects)
        {
            effect.Run(args);
        }
    }

    private void Announce(EntityUid map, ADTLavalandEventPrototype proto)
    {
        if (!TryComp<MapComponent>(map, out var mapComp))
            return;

        var filter = Filter.BroadcastMap(mapComp.MapId);

        if (proto.Announcement is { } announcement)
        {
            var message = Loc.GetString(announcement);
            _chat.ChatMessageToManyFiltered(filter, ChatChannel.Radio, message, message, map, false, true, null);
        }

        if (proto.Sound != null)
            _audio.PlayGlobal(proto.Sound, filter, true);
    }

    private bool TryPickEvent(List<ProtoId<ADTLavalandEventPrototype>> events, out ADTLavalandEventPrototype picked)
    {
        picked = default!;

        var total = 0f;
        var candidates = new List<ADTLavalandEventPrototype>();

        foreach (var id in events)
        {
            if (!_prototype.TryIndex(id, out var proto))
            {
                Log.Error($"Lavaland event scheduler references unknown event {id}.");
                continue;
            }

            var weight = MathF.Max(proto.Weight, 0f);

            if (weight <= 0f)
                continue;

            candidates.Add(proto);
            total += weight;
        }

        if (candidates.Count == 0 || total <= 0f)
            return false;

        var roll = _random.NextFloat(total);

        foreach (var candidate in candidates)
        {
            roll -= MathF.Max(candidate.Weight, 0f);

            if (roll > 0f)
                continue;

            picked = candidate;
            return true;
        }

        picked = candidates[^1];
        return true;
    }

    public override void ScatterSpawn(EntityUid map, EntProtoId prototype, int count, ADTScatterSpawnEffect effect)
    {
        if (count <= 0)
            return;

        var center = TryComp<ADTLavalandGenerationComponent>(map, out var generation)
            ? generation.BaseCenter
            : Vector2.Zero;

        var placed = new List<Vector2>();

        for (var i = 0; i < count; i++)
        {
            if (!TryFindSpot(center, effect, placed, map, out var coords))
            {
                Log.Warning($"Lavaland event found nowhere to put {prototype} on {ToPrettyString(map)}.");
                continue;
            }

            placed.Add(coords.Position);
            Spawn(prototype, coords);
        }
    }

    private bool TryFindSpot(
        Vector2 center,
        ADTScatterSpawnEffect effect,
        List<Vector2> placed,
        EntityUid map,
        out EntityCoordinates coords)
    {
        var spacingSq = effect.MinSpacing * effect.MinSpacing;

        for (var attempt = 0; attempt < MaxAttempts; attempt++)
        {
            var angle = _random.NextFloat(0, MathF.PI * 2);
            var distance = _random.NextFloat(effect.MinDistanceFromCenter, effect.MaxRadius);
            var offset = new Vector2(MathF.Cos(angle) * distance, MathF.Sin(angle) * distance);

            var position = center + offset;
            coords = new EntityCoordinates(map, position);

            if (placed.Any(other => Vector2.DistanceSquared(position, other) < spacingSq))
                continue;

            if (effect.AvoidRooms &&
                _lookup.GetEntitiesInRange(coords, effect.RoomClearance)
                    .Any(e => HasComp<RoomFillComponent>(e) || HasComp<ADTRoomFillComponent>(e)))
            {
                continue;
            }

            return true;
        }

        coords = default;
        return false;
    }
}
