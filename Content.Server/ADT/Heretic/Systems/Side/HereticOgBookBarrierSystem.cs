using Content.Shared.ADT.Heretic.Components.Side;
using Content.Shared.ADT.Heretic.Systems;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Physics.Events;

namespace Content.Server.ADT.Heretic.Systems.Side;

public sealed partial class HereticOgBookBarrierSystem : EntitySystem
{
    [Dependency] private readonly SharedHereticSystem _heretic = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HereticOgBookBarrierComponent, StartCollideEvent>(OnCollide);
    }

    private void OnCollide(Entity<HereticOgBookBarrierComponent> ent, ref StartCollideEvent args)
    {
        var subject = args.OtherEntity;
        if (!HasComp<MobStateComponent>(subject) || TerminatingOrDeleted(subject))
            return;
        if (_heretic.IsHereticOrGhoul(subject))
            return;

        var here = _xform.GetMapCoordinates(ent);
        var there = _xform.GetMapCoordinates(subject);
        var dir = there.Position - here.Position;
        if (dir.LengthSquared() < 0.01f)
            dir = new System.Numerics.Vector2(0f, 1f);

        _xform.SetMapCoordinates(subject, there.Offset(dir.Normalized() * 1.5f));

        var spec = new DamageSpecifier();
        spec.DamageDict.Add("Blunt", 5f);
        _damage.TryChangeDamage(subject, spec, true);

        _popup.PopupEntity(Loc.GetString("heretic-ogbook-repel"), subject, subject);
    }
}