using Content.Shared.ADT.Movement;
using Content.Shared.Movement.Components;

namespace Content.Shared.Movement.Systems;

public abstract partial class SharedMoverController
{
    private EntityQuery<ADTInvertRunComponent> _invertRunQuery;

    public void InitializeInvertRun()
    {
        _invertRunQuery = GetEntityQuery<ADTInvertRunComponent>();

        SubscribeLocalEvent<ADTInvertRunComponent, ComponentStartup>(OnInvertRunStartup);
        SubscribeLocalEvent<ADTInvertRunComponent, ComponentShutdown>(OnInvertRunShutdown);
        SubscribeLocalEvent<RelayInputMoverComponent, ComponentStartup>(OnInvertRunRelayStartup);
    }

    public bool ApplyRunInversion(EntityUid uid, bool walking)
    {
        if (_invertRunQuery.HasComponent(uid))
            return !walking;

        return walking;
    }

    private void OnInvertRunStartup(Entity<ADTInvertRunComponent> ent, ref ComponentStartup args)
    {
        SeedInvertRun(ent.Owner, true);
    }

    private void OnInvertRunShutdown(Entity<ADTInvertRunComponent> ent, ref ComponentShutdown args)
    {
        SeedInvertRun(ent.Owner, false);
    }

    private void OnInvertRunRelayStartup(Entity<RelayInputMoverComponent> ent, ref ComponentStartup args)
    {
        if (!_invertRunQuery.HasComponent(ent.Owner))
            return;

        SeedInvertRun(ent.Owner, true);
    }

    private void SeedInvertRun(EntityUid uid, bool walking)
    {
        if (TerminatingOrDeleted(uid))
            return;

        HandleRunChange(uid, 0, walking);
    }
}
