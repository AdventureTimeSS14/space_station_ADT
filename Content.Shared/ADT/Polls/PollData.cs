using System.Text.Json.Serialization;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Polls;

[Serializable, NetSerializable]
public enum PollShowMode : byte
{
    Immediately,
    RoundStart,
    RoundEnd,
}

[Serializable, NetSerializable]
public enum PollQuestionType : byte
{
    Single,
    Multiple,
    Rating,
    Text,
}

[Serializable, NetSerializable]
public enum PollAntagFilter : byte
{
    Any,
    Only,
    Exclude,
}

[Serializable, NetSerializable]
public sealed class PollQuestion
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public PollQuestionType Type { get; set; }

    [JsonPropertyName("options")]
    public List<string> Options { get; set; } = new();

    [JsonPropertyName("min")]
    public int RatingMin { get; set; } = 1;

    [JsonPropertyName("max")]
    public int RatingMax { get; set; } = 5;

    [JsonPropertyName("required")]
    public bool Required { get; set; } = true;
}

[Serializable, NetSerializable]
public sealed class PollFilters
{
    [JsonPropertyName("jobs")]
    public HashSet<string> Jobs { get; set; } = new();

    [JsonPropertyName("departments")]
    public HashSet<string> Departments { get; set; } = new();

    [JsonPropertyName("species")]
    public HashSet<string> Species { get; set; } = new();

    [JsonPropertyName("antag")]
    public PollAntagFilter Antag { get; set; } = PollAntagFilter.Any;

    [JsonPropertyName("antagRoles")]
    public HashSet<string> AntagRoles { get; set; } = new();

    [JsonPropertyName("minHours")]
    public int? MinPlaytimeHours { get; set; }

    [JsonPropertyName("maxHours")]
    public int? MaxPlaytimeHours { get; set; }

    [JsonPropertyName("tracker")]
    public string? Tracker { get; set; }

    [JsonPropertyName("trackerMinHours")]
    public int? TrackerMinHours { get; set; }

    [JsonPropertyName("trackerMaxHours")]
    public int? TrackerMaxHours { get; set; }

    [JsonPropertyName("requireRound")]
    public bool RequireRound { get; set; }

    public bool NeedsRound()
    {
        return RequireRound
            || Jobs.Count > 0
            || Departments.Count > 0
            || Species.Count > 0
            || AntagRoles.Count > 0
            || Antag != PollAntagFilter.Any;
    }
}

[Serializable, NetSerializable]
public sealed class PollData
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("questions")]
    public List<PollQuestion> Questions { get; set; } = new();

    [JsonPropertyName("filters")]
    public PollFilters Filters { get; set; } = new();

    [JsonPropertyName("servers")]
    public HashSet<string> Servers { get; set; } = new();

    [JsonPropertyName("showMode")]
    public PollShowMode ShowMode { get; set; } = PollShowMode.RoundEnd;

    [JsonPropertyName("createdBy")]
    public Guid CreatedBy { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("endsAt")]
    public DateTime? EndsAt { get; set; }

    [JsonPropertyName("closed")]
    public bool Closed { get; set; }

    public bool IsActive(DateTime now)
    {
        return !Closed && (EndsAt == null || EndsAt.Value > now);
    }

    public bool RunsOn(string server)
    {
        return Servers.Count == 0 || Servers.Contains(server);
    }
}

[Serializable, NetSerializable]
public sealed class PollAnswer
{
    [JsonPropertyName("o")]
    public List<int>? Options { get; set; }

    [JsonPropertyName("r")]
    public int? Rating { get; set; }

    [JsonPropertyName("t")]
    public string? Text { get; set; }

    public bool IsEmpty()
    {
        return (Options == null || Options.Count == 0)
            && Rating == null
            && string.IsNullOrWhiteSpace(Text);
    }
}

[Serializable, NetSerializable]
public sealed class PollSnapshot
{
    [JsonPropertyName("j")]
    public HashSet<string> Jobs { get; set; } = new();

    [JsonPropertyName("d")]
    public HashSet<string> Departments { get; set; } = new();

    [JsonPropertyName("s")]
    public HashSet<string> Species { get; set; } = new();

    [JsonPropertyName("a")]
    public HashSet<string> AntagRoles { get; set; } = new();

    [JsonPropertyName("ant")]
    public bool Antag { get; set; }

    [JsonPropertyName("r")]
    public bool InRound { get; set; }

    [JsonPropertyName("pt")]
    public int PlaytimeMinutes { get; set; }

    [JsonIgnore]
    public Dictionary<string, int> TrackerMinutes { get; set; } = new();

    public PollSnapshot Clone()
    {
        return new PollSnapshot
        {
            Jobs = new HashSet<string>(Jobs),
            Departments = new HashSet<string>(Departments),
            Species = new HashSet<string>(Species),
            AntagRoles = new HashSet<string>(AntagRoles),
            Antag = Antag,
            InRound = InRound,
            PlaytimeMinutes = PlaytimeMinutes,
            TrackerMinutes = new Dictionary<string, int>(TrackerMinutes),
        };
    }
}

[Serializable, NetSerializable]
public sealed class PollSummary
{
    public int Id;

    public string Title = string.Empty;

    public bool Targeted;

    public bool Answered;

    public DateTime? EndsAt;
}
