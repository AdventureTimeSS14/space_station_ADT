using System.Collections.Generic;
using Content.Server.Nuke;
using Content.Server.Popups;
using Content.Shared.ADT.Blob;
using Content.Shared.ADT.Blob.Components;
using Content.Shared.Nuke;
using Content.Shared.Popups;
using Robust.Server.GameObjects;
using Robust.Shared.Collections;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;

namespace Content.Server.ADT.Blob;

public sealed class BlobNukeSystem : EntitySystem
{
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly NukeSystem _nuke = default!;
    [Dependency] private readonly PopupSystem _popup = default!;

    private EntityQuery<BlobTileComponent> _tileQuery;
    private readonly Dictionary<EntityUid, float> _infestedArmedNukes = new();

    public override void Initialize()
    {
        base.Initialize();

        _tileQuery = GetEntityQuery<BlobTileComponent>();

        SubscribeLocalEvent<NukeComponent, MoveEvent>(OnNukeMoved);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_infestedArmedNukes.Count == 0)
            return;

        var toRemove = new ValueList<EntityUid>();

        foreach (var (nukeUid, timer) in _infestedArmedNukes)
        {
            if (TerminatingOrDeleted(nukeUid) || !TryComp<NukeComponent>(nukeUid, out var nuke))
            {
                toRemove.Add(nukeUid);
                continue;
            }

            if (!_appearance.TryGetData<bool>(nukeUid, BlobNukeVisuals.Infested, out var infested) || !infested || nuke.Status != NukeStatus.ARMED)
            {
                toRemove.Add(nukeUid);
                continue;
            }

            var newTimer = timer + frameTime;
            if (newTimer >= 60f)
            {
                toRemove.Add(nukeUid);
                _nuke.DisarmBomb(nukeUid, nuke);
                _popup.PopupEntity(Loc.GetString("blob-nuke-disarmed-by-blob"), nukeUid, PopupType.LargeCaution);
            }
            else
            {
                _infestedArmedNukes[nukeUid] = newTimer;
            }
        }

        foreach (var uid in toRemove)
        {
            _infestedArmedNukes.Remove(uid);
        }
    }

    public float? GetDisarmProgress(EntityUid nukeUid)
    {
        if (_infestedArmedNukes.TryGetValue(nukeUid, out var timer))
            return timer;
        return null;
    }

    private void OnNukeMoved(EntityUid uid, NukeComponent component, ref MoveEvent args)
    {
        if (TerminatingOrDeleted(uid))
            return;

        UpdateNukeInfestation(uid, component);
    }

    public void UpdateNukeInfestation(EntityUid nukeUid, NukeComponent? nukeComp = null, TransformComponent? xform = null, EntityUid? terminatingTile = null)
    {
        if (TerminatingOrDeleted(nukeUid))
            return;

        if (!Resolve(nukeUid, ref nukeComp, ref xform))
            return;

        if (xform.GridUid is not { } gridUid || TerminatingOrDeleted(gridUid) || !TryComp<MapGridComponent>(gridUid, out var grid))
        {
            if (!TerminatingOrDeleted(nukeUid))
                _appearance.SetData(nukeUid, BlobNukeVisuals.Infested, false);
            _infestedArmedNukes.Remove(nukeUid);
            return;
        }

        var tileIndices = _map.TileIndicesFor(gridUid, grid, xform.Coordinates);
        var anchored = _map.GetAnchoredEntities(gridUid, grid, tileIndices);

        var isInfested = false;
        foreach (var entity in anchored)
        {
            if (entity == terminatingTile || TerminatingOrDeleted(entity))
                continue;

            if (_tileQuery.TryComp(entity, out var blobTile) && blobTile.Core != null)
            {
                isInfested = true;
                break;
            }
        }

        if (!TerminatingOrDeleted(nukeUid))
            _appearance.SetData(nukeUid, BlobNukeVisuals.Infested, isInfested);

        if (isInfested && nukeComp.Status == NukeStatus.ARMED)
        {
            _infestedArmedNukes.TryAdd(nukeUid, 0f);
        }
        else if (!isInfested || nukeComp.Status != NukeStatus.ARMED)
        {
            _infestedArmedNukes.Remove(nukeUid);
        }
    }

    public void CheckTileForNukes(EntityUid blobTileUid, bool terminating = false)
    {
        if (!TryComp<TransformComponent>(blobTileUid, out var xform))
            return;

        if (xform.MapUid is not { } mapUid || TerminatingOrDeleted(mapUid))
            return;

        if (xform.GridUid is not { } gridUid || TerminatingOrDeleted(gridUid))
            return;

        if (!HasComp<BroadphaseComponent>(gridUid))
            return;

        foreach (var nuke in _lookup.GetEntitiesInRange<NukeComponent>(xform.Coordinates, 0.75f))
        {
            if (TerminatingOrDeleted(nuke.Owner))
                continue;

            UpdateNukeInfestation(nuke.Owner, nuke.Comp, terminatingTile: terminating ? blobTileUid : null);
        }
    }
}
