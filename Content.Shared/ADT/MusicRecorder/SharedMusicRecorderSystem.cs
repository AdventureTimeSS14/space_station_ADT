using Content.Shared.Containers.ItemSlots;

namespace Content.Shared.ADT.MusicRecorder;

public abstract class SharedMusicRecorderSystem : EntitySystem
{
    [Dependency] private readonly ItemSlotsSystem _slots = default!;

    protected bool TryGetCassette(EntityUid recorder, out EntityUid cassette)
    {
        if (_slots.GetItemOrNull(recorder, MusicRecorderComponent.CassetteSlotId) is { } item)
        {
            cassette = item;
            return true;
        }

        cassette = default;
        return false;
    }
}
