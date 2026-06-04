using Robust.Shared.Audio.Systems;
using Content.Shared.ADT.Audio.Jukebox;
using Content.Shared.Interaction;
using Content.Shared.Containers.ItemSlots;
using Robust.Shared.Prototypes;
using Robust.Shared.Containers;

namespace Content.Shared.Audio.Jukebox;

public abstract class SharedJukeboxSystem : EntitySystem
{
    /// ADT-Tweak start
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] protected readonly SharedAudioSystem Audio = default!;

    public const string CassetteSlotId = "music_disk";
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<JukeboxComponent, EntInsertedIntoContainerMessage>(OnContainerInserted);
        SubscribeLocalEvent<JukeboxComponent, EntRemovedFromContainerMessage>(OnContainerRemoved);
    }

    public static float MapToRange(float value, float leftMin, float leftMax, float rightMin, float rightMax) /// ADT-Tweak
    {
        return rightMin + (value - leftMin) * (rightMax - rightMin) / (leftMax - leftMin);
    }

    private void OnContainerInserted(Entity<JukeboxComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != CassetteSlotId)
            return;

        OnItemSlot(ent);
    }

    private void OnContainerRemoved(Entity<JukeboxComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != CassetteSlotId)
            return;

        OnItemSlot(ent);
    }

    private void OnItemSlot(Entity<JukeboxComponent> ent)
    {
        ent.Comp.SelectedSongId = null;
        UpdateMusicList(ent);
        StopJukebox(ent);
        Dirty(ent);

    }

    public EntityUid? GetInsertedDisk(Entity<JukeboxComponent> ent)
    {
        if (!TryComp<ItemSlotsComponent>(ent, out var itemSlots))
            return null;

        return _itemSlots.GetItemOrNull(ent.Owner, CassetteSlotId, itemSlots);
    }

    public JukeboxListPrototype? GetDiskCollection(Entity<JukeboxComponent> ent)
    {
        var diskEntity = GetInsertedDisk(ent);
        if (diskEntity == null)
            return null;

        if (!TryComp<JukeboxDiskComponent>(diskEntity, out var diskComp))
            return null;

        if (string.IsNullOrEmpty(diskComp.Collection))
            return null;

        _protoManager.Resolve(diskComp.Collection, out JukeboxListPrototype? collection);
        return collection;
    }

    public List<ProtoId<JukeboxPrototype>> GetAvailableSongs(Entity<JukeboxComponent> ent)
    {
        var collection = GetDiskCollection(ent);
        if (collection == null)
            return new List<ProtoId<JukeboxPrototype>>();

        return collection.Jukeboxes;
    }

    public bool IsSongAvailable(Entity<JukeboxComponent> ent, ProtoId<JukeboxPrototype> songId)
    {
        var availableSongs = GetAvailableSongs(ent);
        return availableSongs.Contains(songId);
    }

    protected virtual void StopJukebox(Entity<JukeboxComponent> ent) { }
    protected virtual void UpdateMusicList(Entity<JukeboxComponent> ent) { }
    /// ADT-Tweak end
}
