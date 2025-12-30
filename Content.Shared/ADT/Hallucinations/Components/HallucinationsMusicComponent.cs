using Content.Shared.Destructible.Thresholds;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Shizophrenia;

/// <summary>
/// Component added to hallucinating entity to store music
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class HallucinationsMusicComponent : Component
{
    [ViewVariables, AutoNetworkedField]
    public Dictionary<string, HalluciantionMusic> Music = new();

    /// <summary>
    /// Current stored audio streams
    /// CLIENT-ONLY
    /// </summary>
    [ViewVariables]
    public Dictionary<string, EntityUid> ActiveMusic = new();

    /// <summary>
    /// Timers for next music
    /// CLIENT-ONLY
    /// </summary>
    [ViewVariables]
    public Dictionary<string, TimeSpan> NextMusic = new();
}

[Serializable, NetSerializable]
public sealed class HalluciantionMusic
{
    public SoundSpecifier Sound = default!;
    public MinMax? Delay;

    public HalluciantionMusic(SoundSpecifier sound, MinMax? delay)
    {
        Sound = sound;
        Delay = delay;
    }
}
