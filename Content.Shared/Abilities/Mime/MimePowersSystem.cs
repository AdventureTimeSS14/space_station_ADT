using Content.Shared.Popups;
using Content.Shared.Actions;
using Content.Shared.Actions.Events;
using Content.Shared.Alert;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.IdentityManagement;
using Content.Shared.Maps;
using Content.Shared.Paper;
using Content.Shared.Physics;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Map.Components;
using Content.Shared.ADT.Mime;

namespace Content.Shared.Abilities.Mime;

public sealed partial class MimePowersSystem : EntitySystem
{
    public static readonly EntProtoId MutedEffect = "StatusEffectMimeMuted";

    [Dependency] private SharedPopupSystem _popupSystem = default!;
    [Dependency] private SharedActionsSystem _actionsSystem = default!;
    [Dependency] private AlertsSystem _alertsSystem = default!;
    [Dependency] private StatusEffectsSystem _statusEffects = default!;
    [Dependency] private TurfSystem _turf = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedMapSystem _mapSystem = default!; // ADT-Tweak

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MimePowersComponent, MapInitEvent>(OnMapInit);
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
            _popupSystem.PopupEntity(Loc.GetString("mime-ready-to-repent"), uid, uid);
        }
    }

    private void OnMapInit(Entity<MimePowersComponent> ent, ref MapInitEvent args)
    {
        if (!ent.Comp.VowBroken)
            _statusEffects.TrySetStatusEffectDuration(ent, MutedEffect);

        if (ent.Comp.PreventWriting)
        {
            EnsureComp<BlockWritingComponent>(ent, out var illiterateComponent);
            illiterateComponent.FailWriteMessage = ent.Comp.FailWriteMessage;
            Dirty(ent, illiterateComponent);
        }

        _actionsSystem.AddAction(ent, ref ent.Comp.InvisibleWallActionEntity, ent.Comp.InvisibleWallAction);
    }

    private void OnComponentShutdown(Entity<MimePowersComponent> ent, ref ComponentShutdown args)
    {
        _statusEffects.TryRemoveStatusEffect(ent, MutedEffect);
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
        var coords = xform.Coordinates.Offset(offsetValue).SnapToGrid(EntityManager);
        var tile = _turf.GetTileRef(coords);
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

            var targetTile = _mapSystem.GetTileRef(gridUid, mapGrid, targetIndex);

            if (targetTile.Tile.IsEmpty)
                continue;

            if (_turf.IsTileBlocked(targetTile, CollisionGroup.Impassable | CollisionGroup.Opaque))
                continue;

            var coords = _mapSystem.GridTileToLocal(gridUid, mapGrid, targetIndex);
            wallPositions.Add(coords);
        }

        if (wallPositions.Count == 0)
        {
            _popupSystem.PopupEntity(Loc.GetString("mime-invisible-wall-failed"), ent, ent);
            return;
        }
        //ADT-Tweak-End

        var messageSelf = Loc.GetString("mime-invisible-wall-popup-self", ("mime", Identity.Entity(ent.Owner, EntityManager)));
        var messageOthers = Loc.GetString("mime-invisible-wall-popup-others", ("mime", Identity.Entity(ent.Owner, EntityManager)));
        _popupSystem.PopupEntity(messageSelf, messageOthers, ent, ent);

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
        _statusEffects.TryRemoveStatusEffect(uid, MutedEffect);
        if (mimePowers.PreventWriting)
            RemComp<BlockWritingComponent>(uid);

        _alertsSystem.ShowAlert(uid, mimePowers.VowBrokenAlert);
        _actionsSystem.RemoveAction(uid, mimePowers.InvisibleWallActionEntity);

        // ADT-Tweak start
        if (TryComp<MimeFingerGunComponent>(uid, out var fingerGun))
        {
            _actionsSystem.RemoveAction(uid, fingerGun.FingerGunActionEntity);
            Dirty(uid, fingerGun);
        }

        if (TryComp<MimeSilenceComponent>(uid, out var silence))
        {
            _actionsSystem.RemoveAction(uid, silence.SilenceActionEntity);
            Dirty(uid, silence);
        }
        // ADT-Tweak end
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
            _popupSystem.PopupEntity(Loc.GetString("mime-not-ready-repent"), uid, uid);
            return;
        }

        mimePowers.Enabled = true;
        mimePowers.ReadyToRepent = false;
        mimePowers.VowBroken = false;
        Dirty(uid, mimePowers);
        _statusEffects.TrySetStatusEffectDuration(uid, MutedEffect);
        if (mimePowers.PreventWriting)
        {
            EnsureComp<BlockWritingComponent>(uid, out var illiterateComponent);
            illiterateComponent.FailWriteMessage = mimePowers.FailWriteMessage;
            Dirty(uid, illiterateComponent);
        }

        _alertsSystem.ClearAlert(uid, mimePowers.VowBrokenAlert);
        _actionsSystem.AddAction(uid, ref mimePowers.InvisibleWallActionEntity, mimePowers.InvisibleWallAction, uid);

        // ADT-Tweak start
        if (TryComp<MimeFingerGunComponent>(uid, out var fingerGun))
        {
            _actionsSystem.AddAction(uid, ref fingerGun.FingerGunActionEntity, fingerGun.FingerGunAction, uid);
            Dirty(uid, fingerGun);
        }

        if (TryComp<MimeSilenceComponent>(uid, out var silence))
        {
            _actionsSystem.AddAction(uid, ref silence.SilenceActionEntity, silence.SilenceAction, uid);
            Dirty(uid, silence);
        }
        // ADT-Tweak end
    }
}
