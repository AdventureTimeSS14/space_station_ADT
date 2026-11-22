using System.Threading;
using System.Threading.Tasks;
using Content.Server.ADT.Drawing.Database;
using Content.Server.Database;
using Content.Shared.ADT.Drawing;
using Content.Shared.Database;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Server.ADT.Drawing;

public sealed partial class CanvasSessionSystem
{
    private readonly SemaphoreSlim _pyramidLock = new(1, 1);

    private int _pendingPerPlayer;
    private TimeSpan _submitCooldown;
    private int _approvedPerPlayer;

    private void OnSubmit(Entity<CanvasEaselComponent> ent, ref CanvasEaselSubmitMessage args)
    {
        if (!TryComp<ActorComponent>(args.Actor, out var actor) ||
            ent.Comp.Session is not { } uid ||
            !_sessions.TryGetValue(uid, out var session) ||
            session.Host != actor.PlayerSession.UserId ||
            session.State == CanvasSessionState.Loading ||
            session.Submitting)
        {
            return;
        }

        var title = args.Title.Trim();
        var description = args.Description.Trim();
        if (title.Length == 0 || title.Length > CanvasConstants.MaxTitleLength)
        {
            Popup(ent.Owner, args.Actor, "adt-canvas-submit-error-title");
            return;
        }

        if (description.Length > CanvasConstants.MaxDescriptionLength)
        {
            Popup(ent.Owner, args.Actor, "adt-canvas-submit-error-description");
            return;
        }

        session.Submitting = true;
        SubmitAsync(session, actor.PlayerSession, ent.Owner, args.Actor, title, description.Length == 0 ? null : description);
    }

    private async void SubmitAsync(CanvasSession session, ICommonSession player, EntityUid easel, EntityUid actor,
        string title, string? description)
    {
        var user = player.UserId;
        try
        {
            var error = await CheckSubmitAllowed(user);
            if (error != null)
            {
                SubmitPopup(easel, actor, error);
                return;
            }

            for (var attempt = 0; attempt < 3 && !session.Closed && (session.Dirty || session.Saving); attempt++)
            {
                if (!session.Saving)
                    Autosave(session);

                if (_pendingSaves.TryGetValue(session.PaintingUid, out var save))
                    await save;
            }

            if (session.Closed)
                return;

            if (session.Dirty)
            {
                SubmitPopup(easel, actor, "adt-canvas-error-generic");
                return;
            }

            var tiles = new List<PaintingTileRecord>(session.Tiles.Length);
            for (var i = 0; i < session.Tiles.Length; i++)
            {
                tiles.Add(new PaintingTileRecord(session.KeyOf(i), session.Tiles[i].Content));
            }

            var blobs = new Dictionary<CanvasHash, byte[]>(session.Blobs);
            var now = DateTime.UtcNow;
            var revisionId = await _db.SubmitPaintingAsync(session.PaintingUid, title, description, now);
            if (revisionId == null)
            {
                SubmitPopup(easel, actor, "adt-canvas-error-generic");
                return;
            }

            var canvas = session.Canvas;
            var width = session.Width;
            var height = session.Height;
            CloseSession(session, CanvasCloseReason.Submitted);

            if (Exists(canvas))
            {
                var painting = EnsureComp<PaintingComponent>(canvas);
                painting.PaintingUid = session.PaintingUid;
                painting.Size = new Vector2i(width, height);
                painting.Title = title;
                painting.Hidden = false;
                Dirty(canvas, painting);
            }

            if (TryComp<CanvasEaselComponent>(easel, out var easelComp))
                UpdateVisuals((easel, easelComp));

            _adminLog.Add(LogType.CanvasSubmit, LogImpact.Medium,
                $"{player.Name} ({user}) submitted painting {session.PaintingUid} \"{title}\" for moderation");

            SubmitPopup(easel, actor, "adt-canvas-submit-done");
            _moderation.OnSubmitted(session.PaintingUid, title);

            BuildPyramidAsync(session.PaintingUid, revisionId.Value, width, height, tiles, blobs, player);
        }
        catch (Exception e)
        {
            Log.Error($"Failed to submit painting {session.PaintingUid}: {e}");
            SubmitPopup(easel, actor, "adt-canvas-error-generic");
        }
        finally
        {
            session.Submitting = false;
        }
    }

    private async Task<string?> CheckSubmitAllowed(NetUserId user)
    {
        var access = await _moderation.CheckDrawAccess(user.UserId);
        if (access != null)
            return access;

        var now = DateTime.UtcNow;

        var last = await _db.GetLastPaintingSubmitAsync(user.UserId);
        if (last != null && now - last.Value < _submitCooldown)
            return "adt-canvas-submit-error-cooldown";

        if (await _db.CountOwnerPaintingsAsync(user.UserId, AdtPaintingStatus.Submitted) >= _pendingPerPlayer)
            return "adt-canvas-submit-error-pending";

        var approved = await _db.CountOwnerPaintingsAsync(user.UserId, AdtPaintingStatus.Approved) +
                       await _db.CountOwnerPaintingsAsync(user.UserId, AdtPaintingStatus.Hidden);
        if (approved >= _approvedPerPlayer)
            return "adt-canvas-submit-error-approved";

        return null;
    }

    private void SubmitPopup(EntityUid easel, EntityUid actor, string loc)
    {
        if (Exists(easel) && Exists(actor))
            Popup(easel, actor, loc);
    }

    private async void BuildPyramidAsync(Guid paintingUid, int revisionId, int width, int height,
        List<PaintingTileRecord> tiles, Dictionary<CanvasHash, byte[]> blobs, ICommonSession host)
    {
        await _pyramidLock.WaitAsync();
        try
        {
            CanvasPyramid pyramid;
            try
            {
                pyramid = await Task.Run(() => CanvasPyramidBuilder.Build(width, height, tiles, blobs));
            }
            catch (Exception e)
            {
                Log.Error($"Failed to build pyramid for painting {paintingUid}, submission reverted: {e}");
                await _db.RejectPaintingBuildAsync(paintingUid, DateTime.UtcNow);
                _moderation.ApplyWithdrawn(paintingUid);

                if (host.Status == Robust.Shared.Enums.SessionStatus.InGame)
                    _moderation.NotifyPlayer(host, Loc.GetString("adt-canvas-submit-error-broken"));

                return;
            }

            long bytes = 0;
            foreach (var tile in tiles)
            {
                if (tile.Content.Kind == CanvasTileKind.Blob && blobs.TryGetValue(tile.Content.Hash, out var data))
                    bytes += data.Length;
            }

            var newBlobs = new Dictionary<CanvasHash, byte[]>(pyramid.Blobs)
            {
                [pyramid.Thumb] = pyramid.ThumbData,
            };

            foreach (var data in newBlobs.Values)
            {
                bytes += data.Length;
            }

            var locations = await _blobStore.StoreAsync(newBlobs);
            await _db.SavePaintingPyramidAsync(paintingUid, revisionId, pyramid.Tiles, newBlobs, locations, pyramid.Thumb, bytes, DateTime.UtcNow);
            InvalidatePainting(paintingUid, false);
        }
        catch (Exception e)
        {
            Log.Error($"Failed to save pyramid for painting {paintingUid}: {e}");
        }
        finally
        {
            _pyramidLock.Release();
        }
    }

    private void OnWithdraw(Entity<CanvasEaselComponent> ent, ref CanvasEaselWithdrawMessage args)
    {
        if (!TryComp<ActorComponent>(args.Actor, out var actor))
            return;

        WithdrawAsync(ent.Owner, args.Actor, actor.PlayerSession, args.Painting);
    }

    private async void WithdrawAsync(EntityUid easel, EntityUid actor, ICommonSession player, Guid painting)
    {
        try
        {
            if (!await _db.WithdrawPaintingAsync(painting, player.UserId, DateTime.UtcNow))
            {
                SubmitPopup(easel, actor, "adt-canvas-withdraw-failed");
                return;
            }

            _moderation.ApplyWithdrawn(painting);
            SubmitPopup(easel, actor, "adt-canvas-withdraw-done");

            if (Exists(easel) && _ui.IsUiOpen(easel, CanvasEaselUiKey.Key, actor))
                SendDraftsAsync(easel, actor, player.UserId);
        }
        catch (Exception e)
        {
            Log.Error($"Failed to withdraw painting {painting}: {e}");
            SubmitPopup(easel, actor, "adt-canvas-error-generic");
        }
    }
}
