using Robust.Shared.GameObjects;

namespace Content.Shared.ADT.Mining;

/// <summary>
///     Raised undirected whenever ore entities spawn from a mined vein.
/// </summary>
[ByRefEvent]
public readonly record struct OreMinedEvent(int Amount);
