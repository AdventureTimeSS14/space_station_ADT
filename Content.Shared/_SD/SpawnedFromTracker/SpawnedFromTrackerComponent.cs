using Robust.Shared.GameStates;

namespace Content.Shared.SD.SpawnedFromTracker;

[RegisterComponent, NetworkedComponent]
public sealed partial class SpawnedFromTrackerComponent : Component
{
    public EntityUid SpawnedFrom;
}
