using Content.Shared.Mind;
using Robust.Shared.Containers;
using Robust.Shared.Timing;
using Content.Shared.TransformAnimation;

namespace Content.Server.TransformAnimation;

public sealed partial class TransformAnimationSystem : EntitySystem
{
    #region Dependency
    [Dependency] private readonly SharedMindSystem _mindSystem = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    #endregion

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TransformAnimationComponent, MapInitEvent>(OnMapInit);
    }


    private void OnMapInit(EntityUid uid, TransformAnimationComponent component, MapInitEvent args)
    {
        component.TransformTime = _gameTiming.CurTime + TimeSpan.FromSeconds(component.Duration);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<TransformAnimationComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (_gameTiming.CurTime >= comp.TransformTime && comp.TransformTime != TimeSpan.Zero)
                Transform(uid, comp);
        }
    }

    private void Transform(EntityUid uid, TransformAnimationComponent component)
    {
        var entity = Spawn(component.TransformingTo, Transform(uid).Coordinates);
        if (_mindSystem.TryGetMind(uid, out var mindId, out var mind))
            _mindSystem.TransferTo(mindId, entity);
        QueueDel(uid);
    }

}
