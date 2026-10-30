using Content.Shared.ADT.Heretic.Components.Moon;
using Content.Shared.Chat;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.ADT.Heretic.Systems.Moon;

public sealed class SharedMoonHallucinationSystem : EntitySystem
{
    private static readonly EntProtoId ApparitionPrototype = "MobMoonApparition";

    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedChatSystem _chat = default!;
    [Dependency] private INetManager _net = default!;

    private TimeSpan _nextMove = TimeSpan.Zero;

    public void CauseHallucinations(EntityUid victim, int count = 2, float duration = 15f, bool emote = true)
    {
        if (!_net.IsServer)
            return;

        if (TerminatingOrDeleted(victim))
            return;

        var coords = _transform.GetMapCoordinates(victim);
        if (coords.MapId == MapId.Nullspace)
            return;

        for (var i = 0; i < count; i++)
        {
            var offset = _random.NextVector2(-2.5f, 2.5f);
            var app = Spawn(ApparitionPrototype, new MapCoordinates(coords.Position + offset, coords.MapId));
            var comp = EnsureComp<MoonHallucinationComponent>(app);
            comp.Duration = TimeSpan.FromSeconds(duration);
            comp.DespawnAt = _timing.CurTime + comp.Duration;
            comp.Victim = GetNetEntity(victim);
            Dirty(app, comp);
        }

        if (emote)
            _chat.TryEmoteWithoutChat(victim, "Laugh");
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_net.IsServer)
            return;

        var now = _timing.CurTime;
        var move = now >= _nextMove;
        if (move)
            _nextMove = now + TimeSpan.FromSeconds(0.5);

        var query = EntityQueryEnumerator<MoonHallucinationComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var comp, out _))
        {
            if (now >= comp.DespawnAt)
            {
                QueueDel(uid);
                continue;
            }

            if (comp.Victim == null
                || !TryGetEntity(comp.Victim.Value, out var victim)
                || TerminatingOrDeleted(victim.Value))
            {
                QueueDel(uid);
                continue;
            }

            if (!move)
                continue;

            var coords = _transform.GetMapCoordinates(victim.Value);
            if (coords.MapId == MapId.Nullspace)
            {
                QueueDel(uid);
                continue;
            }

            var offset = _random.NextVector2(-3f, 3f);
            _transform.SetMapCoordinates(uid, new MapCoordinates(coords.Position + offset, coords.MapId));
        }
    }
}
