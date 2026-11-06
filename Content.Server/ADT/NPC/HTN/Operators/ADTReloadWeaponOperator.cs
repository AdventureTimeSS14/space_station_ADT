using Content.Server.NPC;
using Content.Server.NPC.HTN;
using Content.Server.NPC.HTN.PrimitiveTasks;

namespace Content.Server.ADT.NPC.HTN.Operators;

public sealed partial class ADTReloadWeaponOperator : HTNOperator
{
    [Dependency] private IEntityManager _entManager = default!;

    private const string TimerKey = "ADTReloadTimer";

    [DataField]
    public ADTNpcWeaponRole Role = ADTNpcWeaponRole.Main;

    [DataField]
    public float MagazineDelay = 1f;

    [DataField]
    public float RoundDelay = 0.8f;

    public override void Startup(NPCBlackboard blackboard)
    {
        base.Startup(blackboard);
        blackboard.SetValue(TimerKey, 0f);
    }

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        var gear = _entManager.System<ADTNpcGearSystem>();

        if (gear.Resolve(owner, Role) is not { } found)
            return HTNOperatorStatus.Failed;

        var gun = found.Item;
        gear.GetAmmo(gun, out var count, out var capacity);

        if (capacity <= 0 || count >= capacity)
            return HTNOperatorStatus.Finished;

        var timer = (blackboard.TryGetValue<float>(TimerKey, out var stored, _entManager) ? stored : 0f) + frameTime;

        if (gear.IsMagazineGun(gun, out _))
        {
            if (timer < MagazineDelay)
            {
                blackboard.SetValue(TimerKey, timer);
                return HTNOperatorStatus.Continuing;
            }

            return gear.SwapMagazine(owner, gun)
                ? HTNOperatorStatus.Finished
                : HTNOperatorStatus.Failed;
        }

        if (!gear.IsLooseGun(gun))
            return HTNOperatorStatus.Failed;

        if (timer < RoundDelay)
        {
            blackboard.SetValue(TimerKey, timer);
            return HTNOperatorStatus.Continuing;
        }

        blackboard.SetValue(TimerKey, 0f);

        if (!gear.LoadRound(owner, gun))
            return count > 0 ? HTNOperatorStatus.Finished : HTNOperatorStatus.Failed;

        return count + 1 >= capacity ? HTNOperatorStatus.Finished : HTNOperatorStatus.Continuing;
    }
}
