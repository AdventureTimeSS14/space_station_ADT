using Robust.Shared.Audio;

namespace Content.Server.ADT.Chat;

/// <summary>
/// Used to override how emote sound plays
/// </summary>
[ByRefEvent]
public record struct OverrideEmoteSoundEvent(SoundSpecifier? Sound, AudioParams Params)
{
    public bool Cancelled = false;
};
