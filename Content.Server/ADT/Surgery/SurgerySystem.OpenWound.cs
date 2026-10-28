using Content.Shared.ADT.Surgery.Components;
using Content.Shared.Body.Components;

namespace Content.Server.ADT.Surgery;

public sealed partial class SurgerySystem
{
    private static readonly TimeSpan OpenWoundCheckInterval = TimeSpan.FromSeconds(1);
    private const float OpenWoundBleedAmount = 1f;

    private TimeSpan _nextOpenWoundCheck;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_gameTiming.CurTime < _nextOpenWoundCheck)
            return;

        _nextOpenWoundCheck = _gameTiming.CurTime + OpenWoundCheckInterval;

        var query = EntityQueryEnumerator<OperatedComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.GraphId == null && !IsBusy(comp))
            {
                RemCompDeferred<OperatedComponent>(uid);
                continue;
            }

            if (!IsWoundOpen(uid, comp)
                || !TryComp<BloodstreamComponent>(uid, out var bloodstream)
                || bloodstream.BleedAmount >= OpenWoundBleedAmount)
                continue;

            _bloodstream.TryModifyBleedAmount((uid, bloodstream), OpenWoundBleedAmount - bloodstream.BleedAmount);
        }
    }

    private bool IsWoundOpen(EntityUid patient, OperatedComponent comp)
    {
        if (!TryGetGraph(comp, out var graph) || comp.CurrentNode == graph.StartNode)
            return false;

        return GetNode(graph, comp.CurrentNode) is { } node && HasAvailableEdge(patient, node);
    }
}
