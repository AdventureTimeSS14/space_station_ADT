using System.Linq;
using System.Numerics;
using Content.Server.Chat.Systems;
using Content.Shared.ADT.Virology.Effects;
using Content.Shared.Chat;
using Content.Shared.EntityEffects;
using Content.Shared.Item;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Throwing;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Random;

namespace Content.Server.ADT.Virology;

public sealed class ADTDiseaseSpeakEffectSystem : EntityEffectSystem<TransformComponent, ADTDiseaseSpeakEvent>
{
    [Dependency] private ChatSystem _chat = default!;
    [Dependency] private IRobustRandom _random = default!;

    protected override void Effect(Entity<TransformComponent> entity, ref EntityEffectEvent<ADTDiseaseSpeakEvent> args)
    {
        var effect = args.Effect;
        if (effect.Messages.Count == 0)
            return;

        var message = Loc.GetString(_random.Pick(effect.Messages));
        var type = effect.Whisper ? InGameICChatType.Whisper : InGameICChatType.Speak;

        _chat.TrySendInGameICMessage(entity.Owner, message, type, hideChat: false, hideLog: true);
    }
}

public sealed class ADTDiseaseMagnetPullEffectSystem : EntityEffectSystem<TransformComponent, ADTDiseaseMagnetPullEvent>
{
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private ThrowingSystem _throwing = default!;

    protected override void Effect(Entity<TransformComponent> entity, ref EntityEffectEvent<ADTDiseaseMagnetPullEvent> args)
    {
        var effect = args.Effect;
        var origin = _transform.GetMapCoordinates(entity.Owner, entity.Comp);

        var items = new HashSet<Entity<ItemComponent>>();
        _lookup.GetEntitiesInRange(entity.Comp.Coordinates, effect.Range, items);

        foreach (var item in items)
        {
            if (item.Owner == entity.Owner || Deleted(item.Owner))
                continue;

            if (!TryComp<PhysicsComponent>(item.Owner, out var physics) || physics.BodyType == BodyType.Static)
                continue;

            Pull(item.Owner, origin.Position, effect.Steps);
        }

        var borgs = new HashSet<Entity<BorgChassisComponent>>();
        _lookup.GetEntitiesInRange(entity.Comp.Coordinates, effect.Range, borgs);

        foreach (var borg in borgs)
        {
            if (borg.Owner == entity.Owner)
                continue;

            Pull(borg.Owner, origin.Position, effect.Steps);
        }
    }

    private void Pull(EntityUid uid, Vector2 target, int steps)
    {
        var position = _transform.GetMapCoordinates(uid).Position;
        var direction = target - position;
        if (direction.LengthSquared() < 0.01f)
            return;

        var distance = Math.Min(direction.Length(), steps);
        _throwing.TryThrow(uid, direction.Normalized() * distance, 4f, animated: false, playSound: false, doSpin: false);
    }
}
