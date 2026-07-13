using Content.Server.NPC;
using Content.Server.NPC.HTN;
using Content.Server.NPC.HTN.Preconditions;
using Robust.Shared.Timing;

namespace Content.Server.ADT.ZombieJump.Preconditions;

public sealed partial class ZombieJumpCooldownPrecondition : HTNPrecondition
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    [DataField("cooldown")]
    public float Cooldown = 10f;

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
    }

    public override bool IsMet(NPCBlackboard blackboard)
    {
        try
        {
            if (blackboard.TryGetValue<double>("LastJumpTime", out var lastJumpTime, _entManager))
            {
                var currentTime = _gameTiming.CurTime.TotalSeconds;
                if (currentTime - lastJumpTime < Cooldown)
                    return false;
            }
            return true;
        }
        catch
        {
            return true;
        }
    }
}
