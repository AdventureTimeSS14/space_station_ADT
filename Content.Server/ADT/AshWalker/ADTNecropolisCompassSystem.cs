using System.Numerics;
using Content.Server.Chat.Managers;
using Content.Shared.ADT.AshWalker;
using Content.Shared.ADT.AshWalker.Components;
using Content.Shared.ADT.UI;
using Content.Shared.Actions;
using Content.Shared.Chat;
using Content.Shared.DoAfter;
using Content.Shared.Popups;
using Robust.Server.GameObjects;
using Robust.Shared.Player;

namespace Content.Server.ADT.AshWalker;

public sealed class ADTNecropolisCompassSystem : EntitySystem
{
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTNecropolisCompassComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ADTNecropolisCompassComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<ADTNecropolisCompassComponent, ADTNecropolisCompassActionEvent>(OnUse);
        SubscribeLocalEvent<ADTNecropolisCompassComponent, ADTNecropolisCompassSelectMessage>(OnPointSelected);
        SubscribeLocalEvent<ADTNecropolisCompassComponent, ADTNecropolisCompassDoAfterEvent>(OnAnswerReady);
    }

    private void OnMapInit(Entity<ADTNecropolisCompassComponent> ent, ref MapInitEvent args)
    {
        _actions.AddAction(ent.Owner, ref ent.Comp.ActionEntity, ent.Comp.ActionId);
    }

    private void OnShutdown(Entity<ADTNecropolisCompassComponent> ent, ref ComponentShutdown args)
    {
        _actions.RemoveAction(ent.Comp.ActionEntity);
    }

    private void OnUse(Entity<ADTNecropolisCompassComponent> ent, ref ADTNecropolisCompassActionEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<ActorComponent>(ent.Owner, out var actor))
            return;

        var points = CollectPoints(ent.Owner);

        if (points.Count == 0)
        {
            _popup.PopupEntity(Loc.GetString("adt-necropolis-compass-nothing"), ent.Owner, ent.Owner);
            return;
        }

        _ui.SetUiState(ent.Owner, ADTNecropolisCompassUiKey.Key, new ADTNecropolisCompassBuiState(points));
        _ui.OpenUi(ent.Owner, ADTNecropolisCompassUiKey.Key, actor.PlayerSession);

        args.Handled = true;
    }

    private void OnPointSelected(Entity<ADTNecropolisCompassComponent> ent, ref ADTNecropolisCompassSelectMessage args)
    {
        if (args.Actor != ent.Owner)
            return;

        var point = GetEntity(args.Point);

        if (!IsValidPoint(ent.Owner, point))
            return;

        var doAfter = new DoAfterArgs(EntityManager, ent.Owner, ent.Comp.Delay,
            new ADTNecropolisCompassDoAfterEvent(), ent.Owner, point)
        {
            BreakOnDamage = false,
            BreakOnMove = false,
            DistanceThreshold = null,
            NeedHand = false,
            BlockDuplicate = true,
            DuplicateCondition = DuplicateConditions.SameEvent,
        };

        if (_doAfter.TryStartDoAfter(doAfter))
            _popup.PopupEntity(Loc.GetString("adt-necropolis-compass-listen"), ent.Owner, ent.Owner);
    }

    private void OnAnswerReady(Entity<ADTNecropolisCompassComponent> ent, ref ADTNecropolisCompassDoAfterEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        if (!TryComp<ActorComponent>(ent.Owner, out var actor))
            return;

        if (args.Target is not { } point || Deleted(point) || !TryComp<ADTPointOfInterestComponent>(point, out var poiComp))
        {
            SendMessage(actor, Loc.GetString("adt-necropolis-compass-destroyed"));
            return;
        }

        if (args.Cancelled)
            return;

        var from = _transform.GetMapCoordinates(ent.Owner);
        var to = _transform.GetMapCoordinates(point);

        var direction = to.MapId != from.MapId
            ? Loc.GetString("adt-direction-far-away")
            : Loc.GetString(DirectionKey(to.Position - from.Position));

        SendMessage(actor, Loc.GetString(
            "adt-necropolis-compass-answer",
            ("place", Loc.GetString(poiComp.Title)),
            ("direction", direction)));
    }

    private bool IsValidPoint(EntityUid user, EntityUid point)
    {
        return HasComp<ADTPointOfInterestComponent>(point)
               && _transform.GetMapId(point) == _transform.GetMapId(user);
    }

    private List<ADTEntityPickerEntry> CollectPoints(EntityUid user)
    {
        var points = new List<ADTEntityPickerEntry>();
        var userMap = _transform.GetMapId(user);

        var query = EntityQueryEnumerator<ADTPointOfInterestComponent>();
        while (query.MoveNext(out var poi, out var poiComp))
        {
            if (_transform.GetMapId(poi) != userMap)
                continue;

            var proto = MetaData(poi).EntityPrototype?.ID;

            points.Add(new ADTEntityPickerEntry(GetNetEntity(poi), Loc.GetString(poiComp.Title), proto));
        }

        return points;
    }

    private void SendMessage(ActorComponent actor, string message)
    {
        var wrapped = Loc.GetString("chat-manager-server-wrap-message", ("message", message));
        _chat.ChatMessageToOne(ChatChannel.Server, message, wrapped, EntityUid.Invalid, false, actor.PlayerSession.Channel);
    }

    private static string DirectionKey(Vector2 delta)
    {
        if (delta.LengthSquared() < 0.5f)
            return "adt-direction-here";

        return delta.ToWorldAngle().GetDir() switch
        {
            Direction.North => "adt-direction-north",
            Direction.NorthEast => "adt-direction-north-east",
            Direction.East => "adt-direction-east",
            Direction.SouthEast => "adt-direction-south-east",
            Direction.South => "adt-direction-south",
            Direction.SouthWest => "adt-direction-south-west",
            Direction.West => "adt-direction-west",
            Direction.NorthWest => "adt-direction-north-west",
            _ => "adt-direction-here",
        };
    }
}
