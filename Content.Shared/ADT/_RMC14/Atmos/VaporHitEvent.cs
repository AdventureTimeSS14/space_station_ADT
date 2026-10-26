// Ported from RMC-14 (https://github.com/RMC-14/RMC-14), MIT License

using Content.Shared.Chemistry.Components;

namespace Content.Shared._RMC14.Atmos;

[ByRefEvent]
public record struct VaporHitEvent(Entity<SolutionComponent> Solution, int Power);