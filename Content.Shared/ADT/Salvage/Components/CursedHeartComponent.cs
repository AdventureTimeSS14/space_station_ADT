using Content.Shared.Actions;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Salvage.Components;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class CursedHeartComponent : Component
{
    [AutoNetworkedField]
    public EntityUid? PumpActionEntity;
    public EntityUid? ToggleActionEntity;

    public TimeSpan LastPump = TimeSpan.Zero;

    [DataField]
    public float MaxDelay = 5f;
    [DataField]
    public bool IsStopped = false;
    [DataField]
    public FixedPoint2? OriginalCritThreshold;
}

public sealed partial class PumpHeartActionEvent : InstantActionEvent;
public sealed partial class ToggleHeartActionEvent : InstantActionEvent;
