using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Paper;

/// <summary>
///     Shared network contract for dynamic field substitution in paper documents.
///     Populated server-side and sent to the client via <c>PaperBoundUserInterfaceState</c>.
///     Any property may be null if the corresponding data is unavailable.
/// </summary>
[Serializable, NetSerializable]
public sealed class PaperFieldContext
{
    /// <summary>Character name from the player's mind. Null if no mind.</summary>
    public string? CharacterName { get; set; }

    /// <summary>Current job title from the player's mind. Null if no mind/job.</summary>
    public string? Job { get; set; }

    /// <summary>Current round time as HH:MM:SS.</summary>
    public string? CurrentTime { get; set; }

    /// <summary>Current in-game date as DD.MM.YYYY.</summary>
    public string? CurrentDate { get; set; }

    /// <summary>Combined CurrentTime and CurrentDate.</summary>
    public string? CurrentDateTime { get; set; }

    /// <summary>Owning station name. Null if entity has no station.</summary>
    public string? StationName { get; set; }

    /// <summary>Localized gender string (Male/Female/Unsexed). Null if no HumanoidProfile.</summary>
    public string? Gender { get; set; }

    /// <summary>Localized species name. Null if no HumanoidProfile.</summary>
    public string? Race { get; set; }
}
