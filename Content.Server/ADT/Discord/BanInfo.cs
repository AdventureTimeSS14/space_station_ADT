using System.Text.Json.Serialization;
using Robust.Shared.Player;

namespace Content.Server.ADT.Discord;

public sealed class BanInfo
{
    public string BanId { get; set; } = default!;
    public string Target { get; set; } = default!;

    [JsonIgnore]
    public ICommonSession? Player { get; set; }
    public string PlayerName
    {
        get
        {
            return Player is not null ? Player.Name : string.Empty;
        }
    }

    public uint Minutes { get; set; } = default!;
    public string Reason { get; set; } = default!;
    public DateTimeOffset? Expires { get; set; }
    public Dictionary<string, string> AdditionalInfo { get; set; } = new Dictionary<string, string>();
}
