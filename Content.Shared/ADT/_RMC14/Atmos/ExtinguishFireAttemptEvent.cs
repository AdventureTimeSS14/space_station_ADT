// Ported from RMC-14 (https://github.com/RMC-14/RMC-14), MIT License

namespace Content.Shared._RMC14.Atmos;

[ByRefEvent]
public record struct ExtinguishFireAttemptEvent(EntityUid Extinguisher, EntityUid Target, bool Cancelled = false);
