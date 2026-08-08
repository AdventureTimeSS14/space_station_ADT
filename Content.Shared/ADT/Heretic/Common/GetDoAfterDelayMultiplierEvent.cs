namespace Content.Shared.ADT.Heretic.Common;

// ADT: from _Shitmed.DoAfter, no limb relay

public sealed class GetDoAfterDelayMultiplierEvent(float multiplier = 1f) : EntityEventArgs
{
    public float Multiplier = multiplier;
}
