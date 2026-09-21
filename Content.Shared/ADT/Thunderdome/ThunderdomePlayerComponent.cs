using Robust.Shared.GameObjects;
using Robust.Shared.Network;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.ADT.Thunderdome;

[RegisterComponent]
public sealed partial class ThunderdomePlayerComponent : Component
{
    [DataField]
    public EntityUid? RuleEntity;

    [DataField]
    public int Kills;

    [DataField]
    public int Deaths;

    [DataField]
    public int CurrentStreak;

    [DataField]
    public int WeaponSelection;

    public NetUserId? OwnerUser;
    public TimeSpan SpawnTime;
    public NetUserId? LastAttacker;
    public TimeSpan LastAttackerTime;
    public bool DeathCounted;

    public EntityUid? LeaveAction;
    public EntityUid? LeaderboardAction;
}
