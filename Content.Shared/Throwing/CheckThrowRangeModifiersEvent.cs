using System.Numerics;  // ADT File

namespace Content.Shared.Throwing;

/// <summary>
///     Raised on thrown entity.
/// </summary>
[ByRefEvent]
public record struct CheckThrowRangeModifiersEvent(EntityUid? User, float VectorMod = 1f, float SpeedMod = 1f);
