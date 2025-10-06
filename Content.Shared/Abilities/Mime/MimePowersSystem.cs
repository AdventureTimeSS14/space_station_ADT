using Content.Shared.Popups;
using Content.Shared.Actions;
using Content.Shared.Actions.Events;
using Content.Shared.Alert;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.IdentityManagement;
using Content.Shared.Maps;
using Content.Shared.Paper;
using Content.Shared.Physics;
using Content.Shared.Speech.Muting;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Timing;
using Robust.Shared.Map.Components;

namespace Content.Shared.Abilities.Mime;

public sealed class MimePowersSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly AlertsSystem _alertsSystem = default!;
    [Dependency] private readonly TurfSystem _turf = default!;
    [Dependency] private readonly IMapManager _mapMan = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MimePowersComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<MimePowersComponent, ComponentShutdown>(OnComponentShutdown);
        SubscribeLocalEvent<MimePowersComponent, InvisibleWallActionEvent>(OnInvisibleWall);

        SubscribeLocalEvent<MimePowersComponent, BreakVowAlertEvent>(OnBreakVowAlert);
        SubscribeLocalEvent<MimePowersComponent, RetakeVowAlertEvent>(OnRetakeVowAlert);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        // Queue to track whether mimes can retake vows yet

        var query = EntityQueryEnumerator<MimePowersComponent>();
        while (query.MoveNext(out var uid, out var mime))
        {
            if (!mime.VowBroken || mime.ReadyToRepent)
                continue;

            if (_timing.CurTime < mime.VowRepentTime)
                continue;

            mime.ReadyToRepent = true;
            Dirty(uid, mime);
            _popupSystem.PopupClient(Loc.GetString("mime-ready-to-repent"), uid, uid);
        }
    }

    private void OnComponentInit(Entity<MimePowersComponent> ent, ref ComponentInit args)
    {
        EnsureComp<MutedComponent>(ent);

        if (ent.Comp.PreventWriting)
        {
            EnsureComp<BlockWritingComponent>(ent, out var illiterateComponent);
            illiterateComponent.FailWriteMessage = ent.Comp.FailWriteMessage;
            Dirty(ent, illiterateComponent);
        }

        _alertsSystem.ShowAlert(ent, ent.Comp.VowAlert);
        _actionsSystem.AddAction(ent, ref ent.Comp.InvisibleWallActionEntity, ent.Comp.InvisibleWallAction);
    }

    private void OnComponentShutdown(Entity<MimePowersComponent> ent, ref ComponentShutdown args)
    {
        _actionsSystem.RemoveAction(ent.Owner, ent.Comp.InvisibleWallActionEntity);
    }

    /// <summary>
    /// Creates an invisible wall in a free space after some checks.
    /// </summary>
    private void OnInvisibleWall(Entity<MimePowersComponent> ent, ref InvisibleWallActionEvent args)
    {
        if (!ent.Comp.Enabled)
            return;

        if (_container.IsEntityOrParentInContainer(ent))
            return;

        var xform = Transform(ent);
        // Get the tile in front of the mime
        var offsetValue = xform.LocalRotation.ToWorldVec();
        var directionPos = xform.Coordinates.Offset(offsetValue).SnapToGrid(EntityManager, _mapMan); //ADT-Tweak
        var tile = _turf.GetTileRef(directionPos);
        if (tile == null)
            return;

        //ADT-Tweak-Start
        var gridUid = tile.Value.GridUid;
        if (!TryComp<MapGridComponent>(gridUid, out var mapGrid))
            return;

        var tileIndex = tile.Value.GridIndices;
        var wallPositions = new List<EntityCoordinates>();

        var dir = xform.LocalRotation.GetCardinalDir();
        var isVertical = dir == Direction.North || dir == Direction.South;

        var half = ent.Comp.WallCount / 2;
        var start = -half;
        var end = ent.Comp.WallCount % 2 == 0 ? half - 1 : half;

        for (int i = start; i <= end; i++)
        {
            var offset = isVertical ? (i, 0) : (0, i);
            var targetIndex = tileIndex + offset;

            var targetTile = mapGrid.GetTileRef(targetIndex);

            if (targetTile.Tile.IsEmpty)
                continue;

            if (_turf.IsTileBlocked(tile.Value, CollisionGroup.Impassable | CollisionGroup.Opaque))
                continue;

            var coords = _mapSystem.GridTileToLocal(gridUid, mapGrid, targetIndex);
            wallPositions.Add(coords);
        }

        if (wallPositions.Count == 0)
        {
            _popupSystem.PopupClient(Loc.GetString("mime-invisible-wall-failed"), ent, ent);
            return;
        }
        //ADT-Tweak-End

        var messageSelf = Loc.GetString("mime-invisible-wall-popup-self", ("mime", Identity.Entity(ent.Owner, EntityManager)));
        var messageOthers = Loc.GetString("mime-invisible-wall-popup-others", ("mime", Identity.Entity(ent.Owner, EntityManager)));
        _popupSystem.PopupPredicted(messageSelf, messageOthers, ent, ent);

        //ADT-Tweak-Start
        foreach (var wallCoords in wallPositions)
        {
            PredictedSpawnAtPosition(ent.Comp.WallPrototype, wallCoords);
        }
        //ADT-Tweak-End

        // Make sure we set the invisible wall to despawn properly
        // PredictedSpawnAtPosition(ent.Comp.WallPrototype, _turf.GetTileCenter(tile.Value)); // ADT-Tweak
        // Handle args so cooldown works
        args.Handled = true;
    }

    private void OnBreakVowAlert(Entity<MimePowersComponent> ent, ref BreakVowAlertEvent args)
    {
        if (args.Handled)
            return;

        BreakVow(ent, ent);
        args.Handled = true;
    }

    private void OnRetakeVowAlert(Entity<MimePowersComponent> ent, ref RetakeVowAlertEvent args)
    {
        if (args.Handled)
            return;

        RetakeVow(ent, ent);
        args.Handled = true;
    }

    /// <summary>
    /// Break this mime's vow to not speak.
    /// </summary>
    public void BreakVow(EntityUid uid, MimePowersComponent? mimePowers = null)
    {
        if (!Resolve(uid, ref mimePowers))
            return;

        if (mimePowers.VowBroken)
            return;

        mimePowers.Enabled = false;
        mimePowers.VowBroken = true;
        mimePowers.VowRepentTime = _timing.CurTime + mimePowers.VowCooldown;
        Dirty(uid, mimePowers);
        RemComp<MutedComponent>(uid);
        if (mimePowers.PreventWriting)
            RemComp<BlockWritingComponent>(uid);

        _alertsSystem.ClearAlert(uid, mimePowers.VowAlert);
        _alertsSystem.ShowAlert(uid, mimePowers.VowBrokenAlert);
        _actionsSystem.RemoveAction(uid, mimePowers.InvisibleWallActionEntity);
    }

    /// <summary>
    /// Retake this mime's vow to not speak.
    /// </summary>
    public void RetakeVow(EntityUid uid, MimePowersComponent? mimePowers = null)
    {
        if (!Resolve(uid, ref mimePowers))
            return;

        if (!mimePowers.ReadyToRepent)
        {
            _popupSystem.PopupClient(Loc.GetString("mime-not-ready-repent"), uid, uid);
            return;
        }

        mimePowers.Enabled = true;
        mimePowers.ReadyToRepent = false;
        mimePowers.VowBroken = false;
        Dirty(uid, mimePowers);
        AddComp<MutedComponent>(uid);
        if (mimePowers.PreventWriting)
        {
            EnsureComp<BlockWritingComponent>(uid, out var illiterateComponent);
            illiterateComponent.FailWriteMessage = mimePowers.FailWriteMessage;
            Dirty(uid, illiterateComponent);
        }

        _alertsSystem.ClearAlert(uid, mimePowers.VowBrokenAlert);
        _alertsSystem.ShowAlert(uid, mimePowers.VowAlert);
        _actionsSystem.AddAction(uid, ref mimePowers.InvisibleWallActionEntity, mimePowers.InvisibleWallAction, uid);
    }
}
