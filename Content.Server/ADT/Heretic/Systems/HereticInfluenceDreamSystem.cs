using System.Linq;
using System.Numerics;
using Content.Server.Chat.Managers;
using Content.Server.Heretic.Components;
using Content.Server.Mind;
using Content.Shared.ADT.Heretic.Systems;
using Content.Shared.Bed.Sleep;
using Content.Shared.Chat;
using Robust.Server.Player;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Server.ADT.Heretic.Systems;

public sealed partial class HereticInfluenceDreamSystem : EntitySystem
{
    [Dependency] private readonly SharedHereticSystem _heretic = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly IPlayerManager _playerMan = default!;
    [Dependency] private readonly IChatManager _chatMan = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;

    private const float MinDreamChance = 0.2f;
    private const float MaxDreamChance = 0.4f;
    private const float MaxDistance = 70f;
    private const float CloseDistance = 12f;
    private const float NearDistance = 30f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SleepingComponent, SleepStateChangedEvent>(OnSleepChanged);
    }

    private void OnSleepChanged(Entity<SleepingComponent> ent, ref SleepStateChangedEvent args)
    {
        if (!args.FellAsleep)
            return;

        if (!_heretic.TryGetHereticComponent(ent.Owner, out _, out _))
            return;

        if (_random.Prob(_random.NextFloat(MinDreamChance, MaxDreamChance)))
            return;

        var here = _xform.GetMapCoordinates(ent.Owner);
        var target = FindNearestShift(here.MapId, here.Position);

        string text;

        if (target == null)
        {
            text = Loc.GetString("heretic-influence-dream-silent");
        }
        else
        {
            var there = _xform.GetMapCoordinates(target.Value);
            var toShift = there.Position - here.Position;
            var distance = toShift.Length();

            var direction = toShift.X switch
            {
                > 0f when MathF.Abs(toShift.Y) < MathF.Abs(toShift.X) * 0.5f => Loc.GetString("heretic-track-east"),
                < 0f when MathF.Abs(toShift.Y) < MathF.Abs(toShift.X) * 0.5f => Loc.GetString("heretic-track-west"),
                > 0f when toShift.Y > 0f => Loc.GetString("heretic-track-northeast"),
                < 0f when toShift.Y > 0f => Loc.GetString("heretic-track-northwest"),
                > 0f => Loc.GetString("heretic-track-southeast"),
                _ => Loc.GetString("heretic-track-southwest"),
            };

            var range = distance switch
            {
                <= CloseDistance => Loc.GetString("heretic-influence-dream-close"),
                <= NearDistance => Loc.GetString("heretic-influence-dream-near"),
                _ => Loc.GetString("heretic-influence-dream-far"),
            };

            text = Loc.GetString("heretic-influence-dream", ("dir", direction), ("range", range));
        }

        if (!_mind.TryGetMind(ent.Owner, out var mindId, out var mind))
            return;

        if (!_playerMan.TryGetSessionById(mind.UserId, out var session))
            return;

        var markup = Loc.GetString("heretic-influence-dream-hypno", ("text", text));

        _chatMan.ChatMessageToOne(ChatChannel.Server, text, markup, default, false, session.Channel);
    }

    private EntityUid? FindNearestShift(MapId map, Vector2 position)
    {
        EntityUid? nearest = null;
        var best = MaxDistance * MaxDistance;

        var query = AllEntityQuery<EldritchInfluenceComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var influence, out var xform))
        {
            if (influence.Spent)
                continue;

            if (xform.MapID != map)
                continue;

            var there = _xform.GetMapCoordinates((uid, xform));
            var distance = (there.Position - position).LengthSquared();
            if (distance >= best)
                continue;

            best = distance;
            nearest = uid;
        }

        return nearest;
    }
}
