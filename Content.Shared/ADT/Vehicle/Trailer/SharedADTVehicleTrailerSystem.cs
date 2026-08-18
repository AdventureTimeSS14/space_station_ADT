using System.Linq;
using System.Numerics;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Buckle;
using Content.Shared.Buckle.Components;
using Content.Shared.Containers;
using Content.Shared.Foldable;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Vehicle.Components;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Network;

namespace Content.Shared.ADT.Vehicle.Trailer;

/// <summary>
/// Событие действия водителя: прицепить или отцепить прицеп от сцепки.
/// </summary>
public sealed partial class ADTTrailerToggleActionEvent : InstantActionEvent
{
}

/// <summary>
/// Сцепки транспорта и прицепы: создание сцепки, прицепление и отцепление каталоги/мешков для трупов.
/// </summary>
public sealed partial class SharedADTVehicleTrailerSystem : EntitySystem
{
    [Dependency] private readonly INetManager _netManager = default!;
    [Dependency] private readonly SharedBuckleSystem _buckle = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTVehicleHitchComponent, MapInitEvent>(OnHitchMapInit);
        SubscribeLocalEvent<ADTVehicleHitchComponent, EntityTerminatingEvent>(OnVehicleTerminating);
        SubscribeLocalEvent<ADTVehicleHitchComponent, StrappedEvent>(OnVehicleStrapped);
        SubscribeLocalEvent<ADTVehicleHitchComponent, UnstrappedEvent>(OnVehicleUnstrapped);
        SubscribeLocalEvent<ADTVehicleHitchComponent, ADTTrailerToggleActionEvent>(OnTrailerToggleAction);

        SubscribeLocalEvent<ADTTrailerComponent, InteractHandEvent>(OnTrailerInteractHand);
        SubscribeLocalEvent<ADTTrailerComponent, FoldedEvent>(OnTrailerFolded);
        SubscribeLocalEvent<ADTTrailerComponent, EntityTerminatingEvent>(OnTrailerTerminating);
    }

    private void OnHitchMapInit(Entity<ADTVehicleHitchComponent> ent, ref MapInitEvent args)
    {
        if (!_netManager.IsServer || ent.Comp.Hitch != null)
            return;

        var hitch = Spawn(ent.Comp.HitchPrototype, Transform(ent).Coordinates);
        if (!TryComp<ADTVehicleHitchStrapComponent>(hitch, out _))
        {
            Log.Error($"Failed to spawn hitch {ent.Comp.HitchPrototype} for {ToPrettyString(ent)}");
            return;
        }

        ent.Comp.Hitch = hitch;
        Dirty(ent);

        _transform.SetCoordinates(hitch, new EntityCoordinates(ent.Owner, ent.Comp.HitchOffset));
    }

    private void OnVehicleTerminating(Entity<ADTVehicleHitchComponent> ent, ref EntityTerminatingEvent args)
    {
        if (ent.Comp.Hitch is not { } hitch)
            return;

        // Выкинуть прицепы на карту, иначе они удалятся вместе с транспортом:
        // при Terminating транспорта ванильный Unbuckle пропускает PlaceNextTo
        if (TryComp<StrapComponent>(hitch, out var strap))
        {
            foreach (var buckled in strap.BuckledEntities.ToArray())
            {
                var xform = Transform(buckled);
                _transform.SetCoordinates(buckled, xform, _transform.ToCoordinates(_transform.ToMapCoordinates(xform.Coordinates)));
                _buckle.Unbuckle(buckled, null);
            }
        }
        // Хич - ребёнок транспорта, движок удалит его сам в RecursiveFlagEntityTermination
    }

    private void OnVehicleStrapped(Entity<ADTVehicleHitchComponent> ent, ref StrappedEvent args)
    {
        var rider = args.Buckle.Owner;
        if (!TryComp<ActionsComponent>(rider, out var actions))
            return;

        _actions.AddAction(rider, ref ent.Comp.ToggleActionEntity, ent.Comp.ToggleAction, ent.Owner, actions);
    }

    private void OnVehicleUnstrapped(Entity<ADTVehicleHitchComponent> ent, ref UnstrappedEvent args)
    {
        _actions.RemoveProvidedActions(args.Buckle.Owner, ent.Owner);
    }

    private void OnTrailerToggleAction(Entity<ADTVehicleHitchComponent> ent, ref ADTTrailerToggleActionEvent args)
    {
        if (_netManager.IsClient)
            return;

        args.Handled = ToggleTrailer(ent, args.Performer);
    }

    private bool ToggleTrailer(Entity<ADTVehicleHitchComponent> ent, EntityUid user)
    {
        // Действие только для текущего водителя этого транспорта
        if (!TryComp<RiderComponent>(user, out var rider) || rider.Vehicle != ent.Owner)
            return false;

        if (ent.Comp.Hitch is not { } hitch || !TryComp<StrapComponent>(hitch, out var strap))
            return false;

        if (strap.BuckledEntities.Count == 0)
        {
            if (!TryFindTrailer(ent, hitch, out var trailer) ||
                !TryComp<BuckleComponent>(trailer, out var buckle) ||
                !_buckle.TryBuckle(trailer, user, hitch, buckle))
            {
                return false;
            }

            _popup.PopupEntity(Loc.GetString("adt-trailer-attached"), ent.Owner, user);
            return true;
        }

        var buckled = strap.BuckledEntities.First();
        // Прямой Unbuckle: CanUnbuckle блокируется коллизией квадроцикла между водителем и сцепкой
        _buckle.Unbuckle(buckled, null);

        // Отодвинуть прицеп от сцепки за корму, чтобы отцепление было заметно
        var away = _transform.GetWorldPosition(hitch) - _transform.GetWorldPosition(ent);
        if (away == Vector2.Zero)
            away = new Vector2(0, 1);
        away = Vector2.Normalize(away);

        _transform.SetWorldPosition(buckled, _transform.GetWorldPosition(hitch) + away * 0.8f);

        _popup.PopupEntity(Loc.GetString("adt-trailer-unattached"), ent.Owner, user);
        return true;
    }

    private void OnTrailerInteractHand(Entity<ADTTrailerComponent> ent, ref InteractHandEvent args)
    {
        if (args.Handled || !_netManager.IsServer)
            return;

        if (!TryComp<BuckleComponent>(ent, out var buckle) || buckle.BuckledTo != null)
            return;

        if (!TryFindHitch(ent.Owner, out var hitch))
            return;

        if (!_buckle.TryBuckle(ent.Owner, args.User, hitch, buckle))
            return;

        args.Handled = true;
        _popup.PopupEntity(Loc.GetString("adt-trailer-attached"), ent.Owner, args.User);
    }

    private void OnTrailerFolded(Entity<ADTTrailerComponent> ent, ref FoldedEvent args)
    {
        if (!args.IsFolded || !_netManager.IsServer)
            return;

        if (!TryComp<BuckleComponent>(ent, out var buckle) || buckle.BuckledTo == null)
            return;

        _buckle.Unbuckle(ent.Owner, null);
    }

    private void OnTrailerTerminating(Entity<ADTTrailerComponent> ent, ref EntityTerminatingEvent args)
    {
        // Убрать прицеп из списка сцепки, чтобы не осталось мёртвого uid
        if (TryComp<BuckleComponent>(ent, out var buckle) && buckle.BuckledTo != null)
            _buckle.Unbuckle(ent.Owner, null);
    }

    private bool TryFindTrailer(Entity<ADTVehicleHitchComponent> ent, EntityUid hitch, out EntityUid trailer)
    {
        trailer = default;
        var hitchPos = _transform.ToMapCoordinates(Transform(hitch).Coordinates);
        var maxRangeSq = ent.Comp.AttachRange * ent.Comp.AttachRange;

        var bestDist = maxRangeSq;
        var query = EntityQueryEnumerator<ADTTrailerComponent, BuckleComponent, TransformComponent>();
        while (query.MoveNext(out var candidate, out _, out var buckle, out var xform))
        {
            if (buckle.BuckledTo != null || _container.IsEntityInContainer(candidate))
                continue;

            if (xform.MapID != hitchPos.MapId)
                continue;

            var dist = (hitchPos.Position - _transform.ToMapCoordinates(xform.Coordinates).Position).LengthSquared();
            if (dist > bestDist)
                continue;

            bestDist = dist;
            trailer = candidate;
        }

        return trailer != default;
    }

    private bool TryFindHitch(EntityUid trailer, out EntityUid hitch)
    {
        hitch = default;
        var trailerPos = _transform.ToMapCoordinates(Transform(trailer).Coordinates);

        var bestDist = float.MaxValue;
        var query = EntityQueryEnumerator<ADTVehicleHitchComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var comp, out var xform))
        {
            if (comp.Hitch is not { } hitchUid)
                continue;

            if (xform.MapID != trailerPos.MapId)
                continue;

            // Только свободная сцепка: один прицеп на одну сцепку
            if (!TryComp<StrapComponent>(hitchUid, out var strap) || strap.BuckledEntities.Count != 0)
                continue;

            var range = comp.AttachRange;
            var dist = (trailerPos.Position - _transform.ToMapCoordinates(xform.Coordinates).Position).LengthSquared();
            if (dist > range * range || dist > bestDist)
                continue;

            bestDist = dist;
            hitch = hitchUid;
        }

        return hitch != default;
    }
}