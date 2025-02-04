using Content.Shared.ADT.Grab;
using Content.Shared.Alert;
using Content.Shared.DoAfter;
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
[Access(typeof(PullingSystem), typeof(SharedHandsSystem))]  // ADT Grab tweaked
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
    public ProtoId<AlertPrototype> PullingAlert = "ADTPulling"; // ADT Grab

    // ADT Grab start
    [ViewVariables]
    public GrabStage Stage
    {
        get => _stage;
        set => _stage = (GrabStage)Math.Clamp((int)value, (int)GrabStage.None, (int)GrabStage.Choke);
    }

    [AutoNetworkedField]
    private GrabStage _stage = GrabStage.None;

    /// <summary>
    /// Specified stats for every grab stage
    /// </summary>
    [DataField]
    public Dictionary<GrabStage, GrabStageStats> GrabStats = new()
    {
        {GrabStage.None, new() { RequiredHands = 1, DoaftersToEscape = 0, MovementSpeedModifier = 0.95f, EscapeAttemptTime = 0f, SetStageTime = 0f }},
        {GrabStage.Soft, new() { RequiredHands = 1, DoaftersToEscape = 1, MovementSpeedModifier = 0.9f, EscapeAttemptTime = 1f, SetStageTime = 0f }},
        {GrabStage.Hard, new() { RequiredHands = 1, DoaftersToEscape = 2, MovementSpeedModifier = 0.8f, EscapeAttemptTime = 1.25f, SetStageTime = 0.75f }},
        {GrabStage.Choke, new() { RequiredHands = 2, DoaftersToEscape = 2, MovementSpeedModifier = 0.65f, EscapeAttemptTime = 1.5f, SetStageTime = 1.25f }}
    };

    /// <summary>
    /// Delay between escape attempts for grabbed person
    /// </summary>
    [DataField]
    public float EscapeAttemptDelay = 0.5f;

    /// <summary>
    /// Virtual items for grab stages that require more than one hand
    /// </summary>
    [ViewVariables]
    [AutoNetworkedField]
    public List<NetEntity> VirtualItems = new();

    [ViewVariables]
    [AutoNetworkedField]
    public TimeSpan NextStageChange = TimeSpan.Zero;

    [ViewVariables]
    public DoAfterId? StageIncreaseDoAfter;

    public int GrabbingDirection = 0; // костыль
    // ADT Grab end
}

public sealed partial class StopPullingAlertEvent : BaseAlertEvent;

// ADT Grab start
public enum GrabStage : int
{
    None = 0,
    Soft = 1,
    Hard = 2,
    Choke = 3
}
// ADT Grab end

