using Content.Shared.ADT.MindShield;
using Content.Shared.Mindshield.Components;
using Robust.Shared.Timing;

namespace Content.Server.ADT.MindShield;

public sealed class MindShieldMalfunctioningSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<MindShieldMalfunctioningComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (component.EndTime <= _timing.CurTime)
                RemCompDeferred(uid, component);
        }
    }

    public static void StartMalfunction(EntityUid uid, float duration)
    {
        var entMan = IoCManager.Resolve<IEntityManager>();
        var timing = IoCManager.Resolve<IGameTiming>();

        if (!entMan.HasComponent<MindShieldComponent>(uid))
            return;

        var malf = new MindShieldMalfunctioningComponent()
        {
            EndTime = timing.CurTime + TimeSpan.FromSeconds(duration)
        };
        entMan.RemoveComponent<MindShieldMalfunctioningComponent>(uid);
        entMan.AddComponent(uid, malf);
    }
}
