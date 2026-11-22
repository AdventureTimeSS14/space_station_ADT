namespace Content.Shared.ADT.Heretic.Common;

// ADT: reuse Blob's BlockTeleportComponent
// only the event lives here, the component is Blob's

[ByRefEvent]
public record struct TeleportAttemptEvent(
    bool Predicted = true,
    string? Message = "teleport-blocked-message",
    bool Cancelled = false);
