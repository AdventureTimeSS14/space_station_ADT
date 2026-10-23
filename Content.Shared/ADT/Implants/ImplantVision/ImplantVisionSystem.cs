using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.ADT.Implants.ImplantVision;

public sealed class ImplantVisionSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;

    public void Add(EntityUid uid, ProtoId<ImplantVisionPrototype> id, TimeSpan? duration = null)
    {
        var comp = EnsureComp<ImplantVisionComponent>(uid);
        var now = _timing.CurTime;
        comp.Effects[id] = new ImplantVisionState
        {
            Start = now,
            End = duration == null ? null : now + duration.Value,
        };
        Dirty(uid, comp);
    }

    public void Remove(EntityUid uid, ProtoId<ImplantVisionPrototype> id)
    {
        if (!TryComp<ImplantVisionComponent>(uid, out var comp) || !comp.Effects.Remove(id))
            return;

        if (comp.Effects.Count == 0)
            RemCompDeferred<ImplantVisionComponent>(uid);
        else
            Dirty(uid, comp);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_net.IsClient)
            return;

        var curTime = _timing.CurTime;
        var query = EntityQueryEnumerator<ImplantVisionComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            List<ProtoId<ImplantVisionPrototype>>? expired = null;
            foreach (var (id, state) in comp.Effects)
            {
                if (state.End is { } end && end <= curTime)
                    (expired ??= new()).Add(id);
            }

            if (expired == null)
                continue;

            foreach (var id in expired)
                Remove(uid, id);
        }
    }
}
