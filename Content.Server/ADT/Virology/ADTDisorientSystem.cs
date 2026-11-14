using Content.Shared.ADT.Virology;
using Content.Shared.ADT.Virology.Components;
using Content.Shared.ADT.Virology.Effects;
using Content.Shared.Movement.Systems;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Virology;

public sealed class ADTDisorientSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedMoverController _mover = default!;

    private static readonly Angle[] Angles =
    {
        Angle.FromDegrees(90),
        Angle.FromDegrees(180),
        Angle.FromDegrees(270),
    };

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTDiseaseCarrierComponent, ADTSymptomEvent<ADTSymptomDisorient>>(OnDisorient);
        SubscribeLocalEvent<ADTDisorientedComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnDisorient(Entity<ADTDiseaseCarrierComponent> ent, ref ADTSymptomEvent<ADTSymptomDisorient> args)
    {
        Disorient(ent.Owner, TimeSpan.FromSeconds(args.Effect.Time));
    }

    public void Disorient(EntityUid uid, TimeSpan duration)
    {
        var end = _timing.CurTime + duration;

        if (TryComp<ADTDisorientedComponent>(uid, out var existing))
        {
            if (existing.EndTime < end)
            {
                existing.EndTime = end;
                Dirty(uid, existing);
            }

            return;
        }

        var angle = _random.Pick(Angles);
        var comp = EnsureComp<ADTDisorientedComponent>(uid);
        comp.EndTime = end;
        comp.Rotation = angle;
        Dirty(uid, comp);

        _mover.RotateCamera(uid, angle);
    }

    private void OnShutdown(Entity<ADTDisorientedComponent> ent, ref ComponentShutdown args)
    {
        if (TerminatingOrDeleted(ent))
            return;

        _mover.RotateCamera(ent.Owner, -ent.Comp.Rotation);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<ADTDisorientedComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (now < comp.EndTime)
                continue;

            RemCompDeferred<ADTDisorientedComponent>(uid);
        }
    }
}
