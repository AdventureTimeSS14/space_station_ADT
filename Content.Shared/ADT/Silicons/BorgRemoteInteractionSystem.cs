using Content.Shared.Interaction;
using Content.Shared.Physics;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.StationAi;
using Content.Shared.Silicons.StationAi;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;

namespace Content.Shared.ADT.Silicons;
public sealed class BorgRemoteInteractionSystem : EntitySystem
{
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedTransformSystem _xforms = default!;

    private EntityQuery<PhysicsComponent> _physicsQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        _physicsQuery = GetEntityQuery<PhysicsComponent>();

        SubscribeLocalEvent<BorgChassisComponent, InRangeOverrideEvent>(OnBorgInRange);
    }

    private void OnBorgInRange(Entity<BorgChassisComponent> ent, ref InRangeOverrideEvent args)
    {
        if (!TryComp(args.Target, out StationAiWhitelistComponent? _))
            return;

        var userXform = Transform(ent.Owner);
        var targetXform = Transform(args.Target);

        if (targetXform.GridUid != userXform.GridUid)
        {
            return;
        }

        var userPos = _xforms.GetMapCoordinates(ent.Owner, userXform);
        var targetPos = _xforms.GetMapCoordinates(args.Target, targetXform);
        var distance = (userPos.Position - targetPos.Position).Length();

        if (distance <= SharedInteractionSystem.InteractionRange)
            return;
    
        var targetEntity = args.Target;
        args.Handled = true;
        args.InRange = _interaction.InRangeUnobstructed(
            userPos,
            targetPos,
            range: 0f,
            predicate: e => e == targetEntity || IsTransparentObstacle(e));
    }

    private bool IsTransparentObstacle(EntityUid entity)
    {
        if (!_physicsQuery.TryComp(entity, out var physics))
            return false;

        var layer = (CollisionGroup)physics.CollisionLayer;

        if (layer == CollisionGroup.GlassLayer || layer == CollisionGroup.GlassAirlockLayer)
            return true;

        return false;
    }
}