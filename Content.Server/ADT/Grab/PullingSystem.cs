using System.Numerics;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Throwing;

namespace Content.Server.ADT.Pulling.Systems;

/// <summary>
/// Server-side extension for PullingSystem.
/// Provides the Throw implementation.
/// </summary>
public sealed partial class ServerPullingSystem : PullingSystem
{
    [Dependency] private readonly ThrowingSystem _throwing = default!;

    public override void Throw(EntityUid thrownUid, EntityUid throwerUid, Vector2 direction, float speed)
    {
        _throwing.TryThrow(thrownUid, direction, speed, animated: false, playSound: false);
    }
}
