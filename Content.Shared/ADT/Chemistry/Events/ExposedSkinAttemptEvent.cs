using Robust.Shared.Localization;

namespace Content.Shared.ADT.Chemistry.Events;

[ByRefEvent]
public record struct ExposedSkinAttemptEvent(EntityUid Used, EntityUid Target)
{
    public bool Cancelled;

    public LocId CancelMessage = "medspray-blocked-suit";
}