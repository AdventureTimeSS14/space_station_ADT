using System.Numerics;
using Content.Shared.Atmos;
using Content.Shared.Physics;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;

namespace Content.Shared.Storage.Components;

/// <summary>
/// A storage component that stores nearby entities in a container when this object is opened or closed.
/// This does not have an UI like grid storage, but just makes them disappear inside.
/// Used for lockers, crates etc.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class EntityStorageComponent : Component, IGasMixtureHolder
{
    /// <summary>
    /// Maximum width or height of an entity allowed inside the storage.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float MaxSize = 1.0f;

    /// <summary>
    /// The delay between opening attempts when stuck inside an entity storage.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan InternalOpenAttemptDelay = TimeSpan.FromSeconds(0.5);

    /// <summary>
    /// The next time a player stuck inside the entity storage can attempt to open it from inside.
    /// </summary>
    [DataField, AutoNetworkedField, AutoPausedField]
    public TimeSpan NextInternalOpenAttempt;

    /// <summary>
    /// Collision masks that get removed when the storage gets opened.
    /// </summary>
    // ADT-Tweak-Start: разрешаем изменение масок коллизий через прототипы
    [DataField]
    public int MasksToRemove = (int)(
        CollisionGroup.MidImpassable |
        CollisionGroup.HighImpassable |
        CollisionGroup.LowImpassable);
    // ADT-Tweak-End

    /// <summary>
    /// Collision masks that were removed from ANY layer when the storage was opened;
    /// </summary>
    [DataField]
    public int RemovedMasks;

    /// <summary>
    /// The total amount of items that can fit in one entitystorage.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int Capacity = 30;

    /// <summary>
    /// Whether or not the entity still has collision when open.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IsCollidableWhenOpen;

    /// <summary>
    /// If true, it opens the storage when the entity inside of it moves.
    /// If false, it prevents the storage from opening when the entity inside of it moves.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool OpenOnMove = true;

    [DataField]
    public Vector2 EnteringOffset = new(0, 0);

    [DataField]
    public CollisionGroup EnteringOffsetCollisionFlags = CollisionGroup.Impassable | CollisionGroup.MidImpassable;

    /// <summary>
    /// How close you have to be to the "entering" spot to be able to enter.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float EnteringRange = 0.18f;

    [DataField]
    public bool ShowContents;

    [DataField]
    public bool OccludesLight = true;

    [DataField]
    public bool DeleteContentsOnDestruction;

    [DataField]
    public bool Airtight = true;

    /// <summary>
    /// Whether or not the entitystorage is open or closed.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Open;

    [DataField]
    public SoundSpecifier CloseSound = new SoundPathSpecifier("/Audio/Effects/closetclose.ogg");

    [DataField]
    public SoundSpecifier OpenSound = new SoundPathSpecifier("/Audio/Effects/closetopen.ogg");

    /// <summary>
    /// Whitelist for what entities are allowed to be inserted into this container.
    /// </summary>
    [DataField]
    public EntityWhitelist? Whitelist = new()
    {
        Components =
        [
            "MobState",
            "Item",
        ],
    };

    /// <summary>
    /// Blacklist for what entities are not allowed to be inserted into this container.
    /// </summary>
    [DataField]
    public EntityWhitelist? Blacklist;

    [ViewVariables]
    public Container Contents = default!;

    /// <summary>
    /// Gas currently contained in this entity storage.
    /// None while open. Grabs gas from the atmosphere when closed, and exposes any entities inside to it.
    /// </summary>
    [DataField]
    public GasMixture Air { get; set; } = new(200);
}

/// <summary>
/// Raised on the entity being inserted whenever checking if an entity can be inserted into an entity storage.
/// </summary>
[ByRefEvent]
public record struct InsertIntoEntityStorageAttemptEvent(BaseContainer Container, EntityUid ItemToInsert, bool Cancelled = false);

/// <summary>
/// Raised on the entity storage whenever checking if an entity can be inserted into it.
/// </summary>
[ByRefEvent]
public record struct EntityStorageInsertedIntoAttemptEvent(BaseContainer Container, EntityUid ItemToInsert, bool Cancelled = false);

/// <summary>
/// Raised on the container's owner whenever an entity storage tries to dump its contents while within a container.
/// </summary>
[ByRefEvent]
public record struct EntityStorageIntoContainerAttemptEvent(BaseContainer Container, bool Cancelled = false);

[ByRefEvent]
public record struct StorageOpenAttemptEvent(EntityUid User, bool Silent, bool Cancelled = false);

[ByRefEvent]
public readonly record struct StorageBeforeOpenEvent;

[ByRefEvent]
public readonly record struct StorageAfterOpenEvent;

[ByRefEvent]
public record struct StorageCloseAttemptEvent(EntityUid? User, bool Cancelled = false);

[ByRefEvent]
public readonly record struct StorageBeforeCloseEvent(HashSet<EntityUid> Contents, HashSet<EntityUid> BypassChecks);

[ByRefEvent]
public readonly record struct StorageAfterCloseEvent;
