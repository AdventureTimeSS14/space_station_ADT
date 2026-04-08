using Content.Shared.Storage.EntitySystems;

namespace Content.Client.Storage.Systems;

<<<<<<< HEAD
public sealed class EntityStorageSystem : SharedEntityStorageSystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<EntityStorageComponent, EntityUnpausedEvent>(OnEntityUnpausedEvent);
        SubscribeLocalEvent<EntityStorageComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<EntityStorageComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<EntityStorageComponent, ActivateInWorldEvent>(OnInteract, after: new[] { typeof(LockSystem) });
        SubscribeLocalEvent<EntityStorageComponent, LockToggleAttemptEvent>(OnLockToggleAttempt);
        SubscribeLocalEvent<EntityStorageComponent, DestructionEventArgs>(OnDestruction);
        SubscribeLocalEvent<EntityStorageComponent, GetVerbsEvent<InteractionVerb>>(AddToggleOpenVerb);
        SubscribeLocalEvent<EntityStorageComponent, ContainerRelayMovementEntityEvent>(OnRelayMovement);
        SubscribeLocalEvent<EntityStorageComponent, FoldAttemptEvent>(OnFoldAttempt);

        SubscribeLocalEvent<EntityStorageComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<EntityStorageComponent, ComponentHandleState>(OnHandleState);
    }

    public override bool ResolveStorage(EntityUid uid, [NotNullWhen(true)] ref EntityStorageComponent? component)
    {
        if (component != null)
            return true;

        TryComp<EntityStorageComponent>(uid, out var storage);
        component = storage;
        return component != null;
    }
}
=======
public sealed class EntityStorageSystem : SharedEntityStorageSystem;
>>>>>>> upstreamwiz/master
