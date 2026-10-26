using System.Numerics;
using Content.Shared.Movement.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Map;

namespace Content.Shared.ADT.TileMovement;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TileMovementComponent : Component
{
    [AutoNetworkedField]
    public bool SlideActive;

    [AutoNetworkedField]
    public EntityCoordinates Origin;

    [AutoNetworkedField]
    public Vector2 Destination;

    [AutoNetworkedField]
    public TimeSpan? MovementKeyInitialDownTime;

    [AutoNetworkedField]
    public MoveButtons CurrentSlideMoveButtons;

    [AutoNetworkedField]
    public bool WasWeightlessLastTick;

    [AutoNetworkedField]
    public bool FailureSlideActive;

    [AutoNetworkedField]
    public Vector2? LastTickLocalCoordinates;
}
