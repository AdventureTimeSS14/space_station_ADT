using System.Numerics;
using Content.Shared.Movement.Pulling.Systems;

namespace Content.Client.ADT.Pulling.Systems;

/// <summary>
/// Client-side extension for PullingSystem.
/// </summary>
public sealed partial class ClientPullingSystem : PullingSystem
{
    public override void Throw(EntityUid thrownUid, EntityUid throwerUid, Vector2 direction, float speed)
    {
        // Client-side throw is a no-op; server handles it.
    }
}
