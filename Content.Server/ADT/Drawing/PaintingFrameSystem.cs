using System.Linq;
using Content.Server.Database;
using Content.Shared.ADT.Drawing;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.GameTicking;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.ADT.Drawing;

public sealed class PaintingFrameSystem : EntitySystem
{
    private static readonly EntProtoId GalleryPainting = "ADTPainting";

    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

    private readonly HashSet<Guid> _usedThisRound = new();
    private readonly Queue<EntityUid> _galleryQueue = new();
    private bool _galleryRunning;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PaintingFrameComponent, EntInsertedIntoContainerMessage>(OnInserted);
        SubscribeLocalEvent<PaintingFrameComponent, EntRemovedFromContainerMessage>(OnRemoved);
        SubscribeLocalEvent<PaintingFrameComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }

    private void OnInserted(Entity<PaintingFrameComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID == ent.Comp.SlotId)
            UpdateAppearance(ent);
    }

    private void OnRemoved(Entity<PaintingFrameComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID == ent.Comp.SlotId)
            UpdateAppearance(ent);
    }

    private void OnRoundRestart(RoundRestartCleanupEvent args)
    {
        _usedThisRound.Clear();
        _galleryQueue.Clear();
    }

    public void UpdateAppearance(Entity<PaintingFrameComponent> ent)
    {
        var painting = string.Empty;
        var hidden = false;

        if (_itemSlots.GetItemOrNull(ent.Owner, ent.Comp.SlotId) is { } item &&
            TryComp<PaintingComponent>(item, out var comp) &&
            comp.PaintingUid is { } uid)
        {
            painting = uid.ToString();
            hidden = comp.Hidden;
            _usedThisRound.Add(uid);
        }

        _appearance.SetData(ent.Owner, PaintingFrameVisuals.Painting, painting);
        _appearance.SetData(ent.Owner, PaintingFrameVisuals.Hidden, hidden);
    }

    public void RefreshFor(Guid paintingUid)
    {
        var query = EntityQueryEnumerator<PaintingFrameComponent>();
        while (query.MoveNext(out var uid, out var frame))
        {
            if (_itemSlots.GetItemOrNull(uid, frame.SlotId) is { } item &&
                TryComp<PaintingComponent>(item, out var painting) &&
                painting.PaintingUid == paintingUid)
            {
                UpdateAppearance((uid, frame));
            }
        }
    }

    public void EjectFromFrame(EntityUid painting)
    {
        if (!_container.TryGetContainingContainer(painting, out var container) ||
            !TryComp<PaintingFrameComponent>(container.Owner, out var frame) ||
            container.ID != frame.SlotId)
        {
            return;
        }

        _container.Remove(painting, container);
    }

    private void OnMapInit(Entity<PaintingFrameComponent> ent, ref MapInitEvent args)
    {
        UpdateAppearance(ent);

        if (!ent.Comp.Gallery || _itemSlots.GetItemOrNull(ent.Owner, ent.Comp.SlotId) != null)
            return;

        _galleryQueue.Enqueue(ent.Owner);
        if (!_galleryRunning)
            ProcessGalleryQueue();
    }

    private async void ProcessGalleryQueue()
    {
        _galleryRunning = true;
        try
        {
            while (_galleryQueue.TryDequeue(out var frameUid))
            {
                if (!TryComp<PaintingFrameComponent>(frameUid, out var frame) ||
                    TerminatingOrDeleted(frameUid) ||
                    _itemSlots.GetItemOrNull(frameUid, frame.SlotId) != null)
                {
                    continue;
                }

                var summary = await _db.GetRandomApprovedPaintingAsync(_usedThisRound.ToList(), frame.PinnedOnly, _random.Next());
                if (summary == null)
                    continue;

                if (!TryComp(frameUid, out frame) ||
                    TerminatingOrDeleted(frameUid) ||
                    _itemSlots.GetItemOrNull(frameUid, frame.SlotId) != null)
                {
                    continue;
                }

                _usedThisRound.Add(summary.PaintingUid);

                var item = Spawn(GalleryPainting, Transform(frameUid).Coordinates);
                var painting = EnsureComp<PaintingComponent>(item);
                painting.PaintingUid = summary.PaintingUid;
                painting.Size = new Vector2i(summary.Width, summary.Height);
                painting.Title = summary.Title;
                Dirty(item, painting);

                if (!_itemSlots.TryInsert(frameUid, frame.SlotId, item, null))
                    QueueDel(item);
            }
        }
        catch (Exception e)
        {
            Log.Error($"Failed to fill gallery frames: {e}");
        }
        finally
        {
            _galleryRunning = false;
        }
    }
}
