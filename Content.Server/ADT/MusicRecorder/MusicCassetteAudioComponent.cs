namespace Content.Server.ADT.MusicRecorder;

/// <summary>
/// OGG-байты треков
/// </summary>
[RegisterComponent]
public sealed partial class MusicCassetteAudioComponent : Component
{
    public Dictionary<string, byte[]> Audio = new();
}
