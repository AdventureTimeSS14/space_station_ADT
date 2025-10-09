namespace Content.Shared.ADT.Crawling;

[ByRefEvent]
public record struct ExplosionDownAttemptEvent(string Explosion, bool Cancelled = false);
