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
