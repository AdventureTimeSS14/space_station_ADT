using Content.Shared.Alert;
using Content.Shared.DoAfter;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Movement.Pulling.Components;

/// <summary>
/// Specifies an entity as being pullable by an entity with <see cref="PullerComponent"/>
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(Systems.PullingSystem))]
public sealed partial class PullableComponent : Component
{
    /// <summary>
    /// The current entity pulling this component.
    /// </summary>
    [AutoNetworkedField, DataField]
    public EntityUid? Puller;

    /// <summary>
    /// The pull joint.
    /// </summary>
    [AutoNetworkedField, DataField]
    public string? PullJointId;

    public bool BeingPulled => Puller != null;

    /// <summary>
    /// If the physics component has FixedRotation should we keep it upon being pulled
    /// </summary>
    [Access(typeof(Systems.PullingSystem), Other = AccessPermissions.ReadExecute)]
    [ViewVariables(VVAccess.ReadWrite), DataField("fixedRotation")]
    public bool FixedRotationOnPull;

    /// <summary>
    /// What the pullable's fixedrotation was set to before being pulled.
    /// </summary>
    [Access(typeof(Systems.PullingSystem), Other = AccessPermissions.ReadExecute)]
    [AutoNetworkedField, DataField]
    public bool PrevFixedRotation;

    [DataField]
    public ProtoId<AlertPrototype> PulledAlert = "Pulled";

    [ViewVariables]
    public TimeSpan LastEscapeAttempt = TimeSpan.Zero;

    [DataField]
    public Dictionary<GrabStage, float> GrabEscapeAttemptTimes = new()
    {
        {GrabStage.None, 0f},
        {GrabStage.Soft, 1f},
        {GrabStage.Hard, 1.5f},
        {GrabStage.Choke, 2f}
    };

    [DataField]
    public Dictionary<GrabStage, int> GrabEscapeAttemptCount = new()
    {
        {GrabStage.None, 0},
        {GrabStage.Soft, 1},
        {GrabStage.Hard, 2},
        {GrabStage.Choke, 2}
    };

    public int EscapeAttemptCounter = 0;

    public DoAfterId? EscapeAttemptDoAfter;
}

public sealed partial class StopBeingPulledAlertEvent : BaseAlertEvent;
