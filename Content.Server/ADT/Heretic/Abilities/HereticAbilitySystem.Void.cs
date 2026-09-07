//

using System.Numerics;
using Content.Server.Atmos.Components;
using Content.Server.Heretic.Components.PathSpecific;
using Content.Server.Magic;
using Content.Shared.Heretic;
using Content.Shared.Movement.Components;
using Content.Shared.Slippery;
using Robust.Shared.Physics.Components;
using Content.Shared.ADT.Heretic.Common;
using Content.Server.Polymorph.Components;
using Content.Shared.ADT.Heretic.Components;
using Content.Shared.Coordinates;
using Content.Shared.Movement.Events;
using Content.Shared.Physics.Controllers;
using Content.Shared.Polymorph;
using Content.Shared.Stunnable;
using Robust.Shared.Physics;
using Robust.Shared.Prototypes;

namespace Content.Server.Heretic.Abilities;

public sealed partial class HereticAbilitySystem
{
    private static readonly EntProtoId<VoidAscensionAuraComponent> VoidAuraId = "VoidAscensionAura";

    protected override void SubscribeVoid()
    {
        base.SubscribeVoid();

        SubscribeLocalEvent<HereticAscensionVoidEvent>(OnAscensionVoid);

        SubscribeLocalEvent<HereticVoidBlastEvent>(OnVoidBlast); // ADT: ice cone

        SubscribeLocalEvent<HereticVoidPrisonEvent>(OnVoidPrison);

        SubscribeLocalEvent<VoidPrisonComponent, PolymorphedEvent>(OnPrisonRevert);
    }

    // ADT: fan of ice projectiles
    private void OnVoidBlast(HereticVoidBlastEvent args)
    {
        if (!TryUseAbility(args))
            return;

        var uid = args.Performer;
        var xform = Transform(uid);
        var (pos, rot) = _transform.GetWorldPositionRotation(xform);
        var forward = rot.ToWorldVec();

        var half = args.ConeAngle / 2f;
        for (var i = 0; i < args.Count; i++)
        {
            var angle = args.Count == 1
                ? Angle.Zero
                : Angle.FromDegrees(-half + args.ConeAngle * i / (args.Count - 1));
            var dir = angle.RotateVec(forward);

            var proj = Spawn(args.Projectile, xform.Coordinates);
            _gun.ShootProjectile(proj, dir, Vector2.Zero, uid, uid, args.Speed);
        }
    }

    private void OnPrisonRevert(Entity<VoidPrisonComponent> ent, ref PolymorphedEvent args)
    {
        if (!args.IsRevert)
            return;

        Spawn(ent.Comp.EndEffect, Transform(ent).Coordinates);
        Voidcurse.DoCurse(args.NewEntity);
    }

    private void OnAscensionVoid(HereticAscensionVoidEvent args)
    {
        if (!args.Negative)
            SpawnAttachedTo(VoidAuraId, args.Heretic.ToCoordinates());
        else
        {
            var childEnumerator = Transform(args.Heretic).ChildEnumerator;
            while (childEnumerator.MoveNext(out var child))
            {
                if (HasComp<VoidAscensionAuraComponent>(child))
                    QueueDel(child);
            }
        }
    }

    private void OnVoidPrison(HereticVoidPrisonEvent args)
    {
        var target = args.Target;

        if (!HasComp<PolymorphableComponent>(target) || HasComp<VoidPrisonComponent>(target))
            return;

        if (!TryUseAbility(args))
            return;

        args.Handled = true;

        var ev = new BeforeCastTouchSpellEvent(target);
        RaiseLocalEvent(target, ev, true);
        if (ev.Cancelled)
            return;

        _poly.PolymorphEntity(target, args.Polymorph);
    }
}
