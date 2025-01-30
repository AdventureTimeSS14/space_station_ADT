using Content.Shared.Alert;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory.VirtualItem;
using Content.Shared.Movement.Pulling.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Movement.Pulling.Components;

/// <summary>
/// Specifies an entity as being able to pull another entity with <see cref="PullableComponent"/>
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(PullingSystem), typeof(SharedHandsSystem))]
public sealed partial class PullerComponent : Component
{
    // My raiding guild
    /// <summary>
    /// Next time the puller can throw what is being pulled.
    /// Used to avoid spamming it for infinite spin + velocity.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, Access(Other = AccessPermissions.ReadWriteExecute)]
    public TimeSpan NextThrow;

    [DataField]
    public TimeSpan ThrowCooldown = TimeSpan.FromSeconds(1);

    // Before changing how this is updated, please see SharedPullerSystem.RefreshMovementSpeed
    public float WalkSpeedModifier => Pulling == default ? 1.0f : 0.95f;

    public float SprintSpeedModifier => Pulling == default ? 1.0f : 0.95f;

    /// <summary>
    /// Entity currently being pulled if applicable.
    /// </summary>
    [AutoNetworkedField, DataField]
    public EntityUid? Pulling;

    /// <summary>
    ///     Does this entity need hands to be able to pull something?
    /// </summary>
    [DataField]
    public bool NeedsHands = true;

    [DataField]
    public ProtoId<AlertPrototype> PullingAlert = "Pulling";

    [DataField]
    public GrabStage Stage = GrabStage.None;

    [DataField]
    public Dictionary<GrabStage, int> RequiredHands = new()
    {
        {GrabStage.None, 1},
        {GrabStage.Soft, 1},
        {GrabStage.Hard, 1},
        {GrabStage.Choke, 2}
    };

    [DataField]
    public float EscapeAttemptDelay = 0.5f;

    [ViewVariables]
    public List<Entity<VirtualItemComponent>> VirtualItems = new();
}

public sealed partial class StopPullingAlertEvent : BaseAlertEvent;

public enum GrabStage : int
{
    None = 0,
    Soft = 1,
    Hard = 2,
    Choke = 3
}
