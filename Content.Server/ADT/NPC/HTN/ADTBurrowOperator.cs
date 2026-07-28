using Content.Server.NPC;
using Content.Server.NPC.HTN;
using Content.Server.NPC.HTN.PrimitiveTasks;
using Content.Shared.Popups;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;

namespace Content.Server.ADT.NPC.HTN;

public sealed partial class ADTBurrowOperator : HTNOperator
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    private SharedAudioSystem _audio = default!;
    private SharedPopupSystem _popup = default!;

    [DataField]
    public float ChaseTime = 10f;

    [DataField]
    public string TimerKey = "ADTBurrowTimer";

    [DataField]
    public LocId Popup = "adt-goldgrub-burrow";

    [DataField]
    public SoundSpecifier? Sound = new SoundPathSpecifier("/Audio/Effects/break_stone.ogg");

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _audio = sysManager.GetEntitySystem<SharedAudioSystem>();
        _popup = sysManager.GetEntitySystem<SharedPopupSystem>();
    }

    public override void Startup(NPCBlackboard blackboard)
    {
        base.Startup(blackboard);
        blackboard.SetValue(TimerKey, ChaseTime);
    }

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        if (!blackboard.TryGetValue<float>(TimerKey, out var timer, _entManager))
            return HTNOperatorStatus.Failed;

        timer -= frameTime;
        blackboard.SetValue(TimerKey, timer);

        if (timer > 0f)
            return HTNOperatorStatus.Continuing;

        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        if (_entManager.Deleted(owner))
            return HTNOperatorStatus.Failed;

        _popup.PopupEntity(Loc.GetString(Popup, ("creature", _entManager.ToPrettyString(owner))), owner);
        _audio.PlayPvs(Sound, owner);

        _entManager.QueueDeleteEntity(owner);
        return HTNOperatorStatus.Finished;
    }

    public override void TaskShutdown(NPCBlackboard blackboard, HTNOperatorStatus status)
    {
        base.TaskShutdown(blackboard, status);

        if (status != HTNOperatorStatus.BetterPlan)
            blackboard.Remove<float>(TimerKey);
    }
}
