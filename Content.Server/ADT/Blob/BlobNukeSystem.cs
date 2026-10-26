using Content.Server.Nuke;
using Content.Server.Popups;
using Content.Shared.ADT.Blob;
using Content.Shared.ADT.Blob.Components;
using Content.Shared.Examine;
using Content.Shared.Nuke;
using Content.Shared.Popups;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Blob;

public sealed class BlobNukeSystem : EntitySystem
{
    private const float DisarmTime = 60f;

    private static readonly TimeSpan TileCheckInterval = TimeSpan.FromSeconds(1);

    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private AppearanceSystem _appearance = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private NukeSystem _nuke = default!;
    [Dependency] private PopupSystem _popup = default!;
    [Dependency] private IGameTiming _timing = default!;

    [Dependency] private EntityQuery<BlobTileComponent> _tileQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BlobNukeComponent, ExaminedEvent>(OnExamined);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<BlobNukeComponent, NukeComponent>();
        while (query.MoveNext(out var uid, out var blobNuke, out var nuke))
        {
            if (_timing.CurTime >= blobNuke.NextTileCheck)
            {
                blobNuke.NextTileCheck = _timing.CurTime + TileCheckInterval;

                if (!IsOnBlobTile(uid, null))
                {
                    UpdateInfestation(uid);
                    continue;
                }
            }

            if (nuke.Status != NukeStatus.ARMED)
            {
                blobNuke.Timer = 0f;
                continue;
            }

            blobNuke.Timer += frameTime;
            if (blobNuke.Timer < DisarmTime)
                continue;

            blobNuke.Timer = 0f;
            _nuke.DisarmBomb(uid, nuke);
            _popup.PopupEntity(Loc.GetString("blob-nuke-disarmed-by-blob"), uid, PopupType.LargeCaution);
        }
    }

    private void OnExamined(Entity<BlobNukeComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("blob-nuke-examined-infested"));

        if (!TryComp<NukeComponent>(ent, out var nuke) || nuke.Status != NukeStatus.ARMED)
            return;

        var remaining = MathF.Max(0f, DisarmTime - ent.Comp.Timer);
        args.PushMarkup(Loc.GetString("blob-nuke-examined-disarming", ("seconds", (int) MathF.Ceiling(remaining))));
    }

    private void UpdateInfestation(EntityUid uid, EntityUid? terminatingTile = null)
    {
        if (TerminatingOrDeleted(uid))
            return;

        var infested = IsOnBlobTile(uid, terminatingTile);

        if (infested)
            EnsureComp<BlobNukeComponent>(uid);
        else
            RemCompDeferred<BlobNukeComponent>(uid);

        _appearance.SetData(uid, BlobNukeVisuals.Infested, infested);
    }

    private bool IsOnBlobTile(EntityUid uid, EntityUid? terminatingTile)
    {
        var xform = Transform(uid);

        if (xform.GridUid is not { } gridUid || TerminatingOrDeleted(gridUid) || !TryComp<MapGridComponent>(gridUid, out var grid))
            return false;

        var tileIndices = _map.TileIndicesFor(gridUid, grid, xform.Coordinates);
        foreach (var entity in _map.GetAnchoredEntities(gridUid, grid, tileIndices))
        {
            if (entity == terminatingTile || TerminatingOrDeleted(entity))
                continue;

            if (_tileQuery.TryComp(entity, out var blobTile) && blobTile.Core != null)
                return true;
        }

        return false;
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
            UpdateInfestation(nuke.Owner, terminating ? blobTileUid : null);
        }
    }
}
