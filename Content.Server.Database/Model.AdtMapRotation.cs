using System;
using Microsoft.EntityFrameworkCore;

namespace Content.Server.Database;

[PrimaryKey(nameof(Server), nameof(MapId))]
public class AdtMapRotation
{
    public string Server { get; set; } = null!;

    public string MapId { get; set; } = null!;

    public double BonusPerPlayer { get; set; }

    public DateTime? LastPlayed { get; set; }
}
