using Content.Shared.ADT.Heretic.Components.Side;
using Content.Shared.ADT.Heretic.Systems;
using Content.Shared.Cuffs.Components;
using Content.Shared.Examine;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Robust.Shared.GameObjects;
using Robust.Shared.Physics.Events;
using Robust.Shared.Timing;
namespace Content.Server.ADT.Heretic.EntitySystems;
public sealed partial class HereticMansusGateSystem : EntitySystem
{
    [Dependency] private readonly SharedHereticSystem _heretic = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    private readonly Dictionary<EntityUid, TimeSpan> _immunity = new();
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HereticMansusGateComponent, StartCollideEvent>(OnCollide);
        SubscribeLocalEvent<HereticMansusGateComponent, ExaminedEvent>(OnExamine);
    }

    private void OnExamine(Entity<HereticMansusGateComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("heretic-gate-examine-move"));
    }
    private void OnCollide(Entity<HereticMansusGateComponent> ent, ref StartCollideEvent args)
    {
        var subject = args.OtherEntity;
        if (!HasComp<MobStateComponent>(subject))
            return;
        if (_immunity.TryGetValue(subject, out var until) && _timing.CurTime < until)
            return;
        if (entManExists(ent, subject))
        {
            if (!CanPass(subject))
            {
                _popup.PopupEntity(Loc.GetString("heretic-gate-refuse"), subject, subject);
                return;
            }

            if (ent.Comp.LinkedGate == null || !Exists(ent.Comp.LinkedGate.Value))
                return;

            if (ent.Comp.LinkedGate.Value == ent.Owner)
                return;

            var dest = Transform(ent.Comp.LinkedGate.Value).Coordinates.Offset(new System.Numerics.Vector2(0.75f, 0f));
            _xform.SetCoordinates(subject, dest);
            _immunity[subject] = _timing.CurTime + TimeSpan.FromSeconds(1);
        }
    }
    private bool entManExists(Entity<HereticMansusGateComponent> ent, EntityUid subject)
    {
        return Exists(ent) && Exists(subject) && !TerminatingOrDeleted(subject);
    }
    private bool CanPass(EntityUid uid)
    {
        if (_heretic.IsHereticOrGhoul(uid))
            return true;
        if (!TryComp<MobStateComponent>(uid, out var state))
            return false;
        if (state.CurrentState == MobState.Dead)
            return true;
        if (state.CurrentState != MobState.Alive)
            return false;
        if (TryComp<CuffableComponent>(uid, out var cuffs) && cuffs.CuffedHandCount > 0)
            return false;
        return true;
    }
}
