namespace Content.Server.ADT.InconnuOS.NanoNet;

public sealed record NanoNetStoredSite(string Label, Guid UserId, string OwnerName, string Html, DateTime PublishedAt);
