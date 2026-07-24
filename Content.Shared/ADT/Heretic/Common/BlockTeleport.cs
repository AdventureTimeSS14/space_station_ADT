using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Heretic.Common;

// ADT: перенесено из Content.Goobstation.Common.BlockTeleport

[RegisterComponent, NetworkedComponent]
public sealed partial class BlockTeleportComponent : Component
{
}

[ByRefEvent]
public record struct TeleportAttemptEvent(
    bool Predicted = true,
    string? Message = "teleport-blocked-message",
    bool Cancelled = false);
