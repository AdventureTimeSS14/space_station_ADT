using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Paper;

[Serializable, NetSerializable]
public sealed class PaperFieldContext
{
    public string? CharacterName { get; set; }
    public string? Job { get; set; }
    public string? CurrentTime { get; set; }
    public string? CurrentDate { get; set; }
    public string? CurrentDateTime { get; set; }
    public string? StationName { get; set; }
    public string? Gender { get; set; }
    public string? Race { get; set; }
}
