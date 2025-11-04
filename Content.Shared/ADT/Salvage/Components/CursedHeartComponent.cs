using Content.Shared.Actions;
using Content.Shared.ADT.Salvage.Systems;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Content.Shared.FixedPoint;

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
public sealed partial class ToggleHeartActionEvent : InstantActionEvent
{

}
