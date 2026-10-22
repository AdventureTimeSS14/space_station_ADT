using Robust.Shared.Timing;

namespace Content.Server.ADT.NPC;

[RegisterComponent]
public sealed partial class ADTNpcCombatMemoryComponent : Component
{
    [ViewVariables]
    public bool Hurt;

    [ViewVariables]
    public TimeSpan LastHurt;

    [ViewVariables]
    public bool Blocked;

    [ViewVariables]
    public TimeSpan LastBlocked;

    [DataField]
    public float AllyClearance = 0.7f;

    [ViewVariables]
    public bool Spoke;

    [ViewVariables]
    public TimeSpan LastSpoke;

    [DataField]
    public float HostileScanRange = 15f;

    [ViewVariables]
    public GameTick HostileTick;

    [ViewVariables]
    public float HostileScannedRange;

    [ViewVariables]
    public float NearestHostile = float.MaxValue;

    [ViewVariables]
    public EntityUid? Leader;

    [ViewVariables]
    public TimeSpan NextLeaderCheck;

    [DataField]
    public TimeSpan LeaderCheckInterval = TimeSpan.FromSeconds(2);

    [ViewVariables]
    public GameTick ObstacleTick;

    [ViewVariables]
    public EntityUid? Obstacle;

    [ViewVariables]
    public TimeSpan NextCoverSearch;
}
