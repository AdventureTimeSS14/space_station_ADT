using Robust.Shared.GameStates;

namespace Content.Shared.ADT.MusicRecorder;

[RegisterComponent, NetworkedComponent]
public sealed partial class MusicRecorderComponent : Component
{
    public const string CassetteSlotId = "cassette";

    public const int MaxCassetteNameLength = 32;
}
