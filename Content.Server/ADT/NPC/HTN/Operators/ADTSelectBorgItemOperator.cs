using System.Threading;
using System.Threading.Tasks;
using Content.Server.NPC;
using Content.Server.NPC.HTN;
using Content.Server.NPC.HTN.PrimitiveTasks;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Item.ItemToggle;
using Content.Shared.PowerCell;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.NPC.HTN.Operators;

public sealed partial class ADTSelectBorgItemOperator : HTNOperator
{
    private ADTNpcGearSystem _gear = default!;
    private SharedHandsSystem _hands = default!;
    private ItemToggleSystem _toggle = default!;
    private PowerCellSystem _powerCell = default!;

    [DataField(required: true)]
    public List<EntProtoId> Items = new();

    [DataField]
    public bool Activate;

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _gear = sysManager.GetEntitySystem<ADTNpcGearSystem>();
        _hands = sysManager.GetEntitySystem<SharedHandsSystem>();
        _toggle = sysManager.GetEntitySystem<ItemToggleSystem>();
        _powerCell = sysManager.GetEntitySystem<PowerCellSystem>();
    }

    public override Task<(bool Valid, Dictionary<string, object>? Effects)> Plan(NPCBlackboard blackboard,
        CancellationToken cancelToken)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (!_gear.IsBorgItemActive(owner, Items) && _gear.FindBorgModule(owner, Items) == null)
            return Task.FromResult<(bool Valid, Dictionary<string, object>? Effects)>((false, null));

        if (Activate && _gear.FindBorgItem(owner, Items) is { } item &&
            !_toggle.IsActivated(item) &&
            !_powerCell.HasActivatableCharge(item))
        {
            return Task.FromResult<(bool Valid, Dictionary<string, object>? Effects)>((false, null));
        }

        return Task.FromResult<(bool Valid, Dictionary<string, object>? Effects)>((true, null));
    }

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (!_gear.SelectBorgItem(owner, Items))
            return HTNOperatorStatus.Failed;

        if (!Activate)
            return HTNOperatorStatus.Finished;

        if (_hands.GetActiveItem(owner) is not { } active)
            return HTNOperatorStatus.Failed;

        if (_toggle.IsActivated(active) || _toggle.TryActivate(active, owner))
            return HTNOperatorStatus.Finished;

        return HTNOperatorStatus.Failed;
    }
}
