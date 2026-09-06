using Content.Shared.ADT.Hallucinations.Components;

namespace Content.Shared.ADT.Hallucinations.Systems;

public sealed partial class BoundHallucinationSystem : EntitySystem
{
    [Dependency] private SharedEyeSystem _eye = default!;
    [Dependency] private SharedTransformSystem _xform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BoundHallucinationComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnShutdown(Entity<BoundHallucinationComponent> ent, ref ComponentShutdown args)
    {
        _eye.SetTarget(ent.Owner, null);
        _eye.SetPvsScale(ent.Owner, 1f);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<BoundHallucinationComponent, HallucinationComponent, TransformComponent, EyeComponent>();
        while (query.MoveNext(out var uid, out _, out var comp, out var xform, out var eye))
        {
            if (comp.Ent is not { Valid: true })
                continue;

            EnsureBoundTarget((uid, eye), comp.Ent);

            if (Transform(comp.Ent).MapID != xform.MapID)
            {
                _eye.SetTarget(uid, null);
                continue;
            }

            _eye.SetOffset(uid, _xform.GetWorldPosition(uid) - _xform.GetWorldPosition(comp.Ent));
        }
    }

    private void EnsureBoundTarget(Entity<EyeComponent> ent, EntityUid target)
    {
        if (ent.Comp.Target == target)
            return;

        _eye.SetTarget(ent.Owner, target);
        _eye.SetPvsScale(ent.Owner, 1.7f);
    }
}
