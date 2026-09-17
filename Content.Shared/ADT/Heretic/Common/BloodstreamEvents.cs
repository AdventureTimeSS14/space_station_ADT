namespace Content.Shared.ADT.Heretic.Common;

// ADT: from Goob Bloodstream

public sealed class StoppedTakingBloodlossDamageEvent : EntityEventArgs;

public sealed class GetBloodlossDamageMultiplierEvent(float multiplier = 1f) : EntityEventArgs
{
    public float Multiplier = multiplier;
}
