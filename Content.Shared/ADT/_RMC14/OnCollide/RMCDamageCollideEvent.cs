// Ported from RMC-14 (https://github.com/RMC-14/RMC-14), MIT License

namespace Content.Shared._RMC14.OnCollide;

[ByRefEvent]
public readonly record struct RMCDamageCollideEvent(EntityUid Target);
