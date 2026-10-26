// Ported from RMC-14 (https://github.com/RMC-14/RMC-14), MIT License
using Robust.Shared.Map;

namespace Content.Shared._RMC14.Line;

public readonly record struct LineTile(EntityCoordinates Coordinates, TimeSpan At);
