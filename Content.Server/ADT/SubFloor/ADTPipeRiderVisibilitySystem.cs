using Content.Shared.Disposal.Unit;
using Content.Shared.Eye;

namespace Content.Server.ADT.SubFloor;

public sealed class ADTPipeRiderVisibilitySystem : EntitySystem
{
    [Dependency] private SharedEyeSystem _eye = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BeingDisposedComponent, ComponentStartup>(OnDisposedStartup);
        SubscribeLocalEvent<BeingDisposedComponent, ComponentRemove>(OnDisposedRemove);

        SubscribeLocalEvent<GetVisMaskEvent>(OnGetVisMask);
    }

    private void OnDisposedStartup(Entity<BeingDisposedComponent> ent, ref ComponentStartup args)
    {
        Refresh(ent);
    }

    private void OnDisposedRemove(Entity<BeingDisposedComponent> ent, ref ComponentRemove args)
    {
        Refresh(ent);
    }

    private void OnGetVisMask(ref GetVisMaskEvent args)
    {
        if (!IsInsideAPipe(args.Entity))
            return;

        args.VisibilityMask |= (int) VisibilityFlags.Subfloor;
    }

    private bool IsInsideAPipe(EntityUid uid)
    {
        return TryComp<BeingDisposedComponent>(uid, out var disposed) &&
               disposed.LifeStage <= ComponentLifeStage.Running;
    }

    private void Refresh(EntityUid uid)
    {
        if (!TryComp<EyeComponent>(uid, out var eye))
            return;

        _eye.RefreshVisibilityMask((uid, eye));
    }
}
