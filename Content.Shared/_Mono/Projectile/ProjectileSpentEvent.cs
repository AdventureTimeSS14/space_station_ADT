namespace Content.Shared._Mono.Projectile;

/// <summary>
/// Raised on a projectile once it has stopped penetrating and is about to be deleted.
/// </summary>
[ByRefEvent]
public record struct ProjectileSpentEvent;
