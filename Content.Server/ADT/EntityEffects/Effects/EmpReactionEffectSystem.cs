using Content.Shared.ADT.EntityEffects.Effects;
using Content.Shared.Emp;
using Content.Shared.EntityEffects;

namespace Content.Server.ADT.EntityEffects.Effects;

public sealed partial class EmpReactionEffectSystem : EntityEffectSystem<TransformComponent, EmpEvent>
{
    [Dependency] private readonly SharedEmpSystem _emp = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    protected override void Effect(Entity<TransformComponent> entity, ref EntityEffectEvent<EmpEvent> args)
    {
        var uid = entity.Owner;
        var ev = args.Effect;
        var xform = entity.Comp;

        var range = Math.Min(ev.RangePerUnit * args.Scale, ev.MaxRange);

        _emp.EmpPulse(
            _transform.GetMapCoordinates(uid, xform),
            range,
            ev.EnergyConsumption,
            TimeSpan.FromSeconds(ev.Duration)
        );
    }
}