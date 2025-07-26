using Robust.Shared.Audio.Systems;

namespace Content.Shared.Audio.Jukebox;

public abstract class SharedJukeboxSystem : EntitySystem
{
    /// ADT-Tweak start
    [Dependency] protected readonly SharedAudioSystem Audio = default!;
    public static float MapToRange(float value, float leftMin, float leftMax, float rightMin, float rightMax) /// ADT-Tweak
    {
        return rightMin + (value - leftMin) * (rightMax - rightMin) / (leftMax - leftMin);
    }
    /// ADT-Tweak end
}
