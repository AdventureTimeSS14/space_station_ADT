using Content.Shared.Body.Components;
<<<<<<< HEAD
using Content.Shared.Body.Events;
=======
>>>>>>> upstreamwiz/master
using Content.Shared.Ghost;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Pointing;

namespace Content.Shared.Body.Systems;

public sealed class BrainSystem : EntitySystem
{
    [Dependency] private readonly SharedMindSystem _mindSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

<<<<<<< HEAD
        SubscribeLocalEvent<BrainComponent, OrganAddedToBodyEvent>((uid, _, args) => HandleMind(args.Body, uid));
        SubscribeLocalEvent<BrainComponent, OrganRemovedFromBodyEvent>((uid, _, args) => HandleMind(uid, args.OldBody));
=======
        SubscribeLocalEvent<BrainComponent, OrganGotInsertedEvent>((uid, _, args) => HandleMind(args.Target, uid));
        SubscribeLocalEvent<BrainComponent, OrganGotRemovedEvent>((uid, _, args) => HandleMind(uid, args.Target));
>>>>>>> upstreamwiz/master
        SubscribeLocalEvent<BrainComponent, PointAttemptEvent>(OnPointAttempt);
    }

    private void HandleMind(EntityUid newEntity, EntityUid oldEntity)
    {
        if (TerminatingOrDeleted(newEntity) || TerminatingOrDeleted(oldEntity))
            return;

        EnsureComp<MindContainerComponent>(newEntity);
        EnsureComp<MindContainerComponent>(oldEntity);

        var ghostOnMove = EnsureComp<GhostOnMoveComponent>(newEntity);
        ghostOnMove.MustBeDead = HasComp<MobStateComponent>(newEntity); // Don't ghost living players out of their bodies.

        if (!_mindSystem.TryGetMind(oldEntity, out var mindId, out var mind))
            return;

        _mindSystem.TransferTo(mindId, newEntity, mind: mind);
    }

    private void OnPointAttempt(Entity<BrainComponent> ent, ref PointAttemptEvent args)
    {
        args.Cancel();
    }
}
