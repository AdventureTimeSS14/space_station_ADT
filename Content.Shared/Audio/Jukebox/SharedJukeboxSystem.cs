using System.Linq;
using Robust.Shared.Audio.Systems;
using Content.Shared.ADT.Audio.Jukebox;
using Content.Shared.Interaction;
using Content.Shared.Containers.ItemSlots;
using Robust.Shared.Prototypes;
using Robust.Shared.Containers;
using Robust.Shared.Random;

namespace Content.Shared.Audio.Jukebox;

public abstract class SharedJukeboxSystem : EntitySystem
{
    /// ADT-Tweak start
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] protected readonly SharedAudioSystem Audio = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public const string DiskSlotId = "music_disk";
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<JukeboxComponent, EntInsertedIntoContainerMessage>(OnContainerInserted);
        SubscribeLocalEvent<JukeboxComponent, EntRemovedFromContainerMessage>(OnContainerRemoved);
        SubscribeLocalEvent<JukeboxDiskComponent, MapInitEvent>(OnComponentInit);
    }

    public static float MapToRange(float value, float leftMin, float leftMax, float rightMin, float rightMax) /// ADT-Tweak
    {
        return rightMin + (value - leftMin) * (rightMax - rightMin) / (leftMax - leftMin);
    }
    private void OnComponentInit(EntityUid uid, JukeboxDiskComponent component, MapInitEvent args)
    {
        InitializeRandomTracks(uid, component);
    }

    private void OnContainerInserted(Entity<JukeboxComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != DiskSlotId)
            return;

        OnItemSlot(ent);
    }

    private void OnContainerRemoved(Entity<JukeboxComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != DiskSlotId)
            return;

        OnItemSlot(ent);
    }

    private void OnItemSlot(Entity<JukeboxComponent> ent)
    {
        ent.Comp.SelectedSongId = null;
        UpdateAppearance(ent);
        UpdateMusicList(ent);
        StopJukebox(ent);
        Dirty(ent);

    }

    public EntityUid? GetInsertedDisk(Entity<JukeboxComponent> ent)
    {
        if (!TryComp<ItemSlotsComponent>(ent, out var itemSlots))
            return null;

        return _itemSlots.GetItemOrNull(ent.Owner, DiskSlotId, itemSlots);
    }

    public JukeboxListPrototype? GetDiskCollection(Entity<JukeboxComponent> ent)
    {
        var diskEntity = GetInsertedDisk(ent);
        if (diskEntity == null)
            return null;

        if (!TryComp<JukeboxDiskComponent>(diskEntity, out var diskComp))
            return null;

        if (string.IsNullOrEmpty(diskComp.TracksCollection))
            return null;

        _protoManager.Resolve(diskComp.TracksCollection, out JukeboxListPrototype? collection);
        return collection;
    }

    public List<ProtoId<JukeboxPrototype>> GetAvailableSongs(Entity<JukeboxComponent> ent)
    {
        var availableSongs = new List<ProtoId<JukeboxPrototype>>();

        var diskEntity = GetInsertedDisk(ent);
        if (diskEntity == null)
            return availableSongs;

        if (!TryComp<JukeboxDiskComponent>(diskEntity, out var diskComp))
            return availableSongs;

        if (!string.IsNullOrEmpty(diskComp.TracksCollection))
        {
            _protoManager.Resolve(diskComp.TracksCollection, out JukeboxListPrototype? collection);
            if (collection != null)
                availableSongs.AddRange(collection.Jukeboxes);
        }

        if (diskComp.Tracks != null)
            availableSongs.AddRange(diskComp.Tracks);

        return availableSongs;
    }

    public bool IsSongAvailable(Entity<JukeboxComponent> ent, ProtoId<JukeboxPrototype> songId)
    {
        var availableSongs = GetAvailableSongs(ent);
        return availableSongs.Contains(songId);
    }

    protected void UpdateAppearance(Entity<JukeboxComponent> ent)
    {
        var hasDisk = TryGetTapeDisk(ent, out _);
        _appearance.SetData(ent.Owner, JukeboxVisuals.HasDisk, hasDisk);
        _appearance.SetData(ent.Owner, JukeboxVisuals.HasDisk, hasDisk);
    }

    public void InitializeRandomTracks(EntityUid uid, JukeboxDiskComponent component)
    {
        if (!component.UseRandom)
            return;

        if (component.Tracks != null && component.Tracks.Count > 0)
            return;

        var allJukeboxes = _protoManager.EnumeratePrototypes<JukeboxPrototype>().ToList();

        if (allJukeboxes.Count == 0)
            return;

        var min = Math.Max(0, component.RandomTracksMin);
        var max = Math.Max(min, component.RandomTracksMax);
        int tracksCount = _random.Next(min, max + 1);

        tracksCount = Math.Min(tracksCount, allJukeboxes.Count);

        _random.Shuffle(allJukeboxes);

        var selectedTracks = allJukeboxes.Take(tracksCount)
                                        .Select(x => new ProtoId<JukeboxPrototype>(x.ID))
                                        .ToList();

        component.Tracks = selectedTracks;
        Dirty(uid, component);
    }

    public static JukeboxVolumeLevel GetVolumeLevel(float volume, float minSlider, float maxSlider)
    {
        var normalized = (volume - minSlider) / (maxSlider - minSlider);
        normalized = Math.Clamp(normalized, 0f, 1f);

        if (normalized < 0.2f)
            return JukeboxVolumeLevel.Level0;
        if (normalized < 0.4f)
            return JukeboxVolumeLevel.Level1;
        if (normalized < 0.6f)
            return JukeboxVolumeLevel.Level2;
        if (normalized < 0.8f)
            return JukeboxVolumeLevel.Level3;
        return JukeboxVolumeLevel.Level4;
    }
    protected bool TryGetTapeDisk(EntityUid ent, out Entity<JukeboxDiskComponent> tape)
    {
        if (_itemSlots.GetItemOrNull(ent, DiskSlotId) is not {} disk)
        {
            tape = default!;
            return false;
        }

        if (!TryComp<JukeboxDiskComponent>(disk, out var comp))
        {
            tape = default!;
            return false;
        }

        tape = new(disk, comp);
        return true;
    }

    protected virtual void StopJukebox(Entity<JukeboxComponent> ent) { }
    protected virtual void UpdateMusicList(Entity<JukeboxComponent> ent) { }
    /// ADT-Tweak end

    /// <summary>
    /// Returns whether or not the given jukebox is currently playing a song.
    /// </summary>
    public bool IsPlaying(Entity<JukeboxComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp))
            return false;

        return entity.Comp.AudioStream is { } audio && Audio.IsPlaying(audio);
    }
}
