using Robust.Shared.GameObjects;

namespace Content.Shared.ADT.Crushers;

public enum TrophyAlteredType : byte
{
    Inserted,
    Removed
}

[ByRefEvent]
public readonly record struct TrophyAlteredEvent(
    EntityUid Holder,
    TrophyAlteredType Alteration
);

[ByRefEvent]
public readonly record struct TrophyHolderTrophiesAlteredEvent(
    EntityUid TrophyUid,
    TrophyAlteredType Alteration
);
