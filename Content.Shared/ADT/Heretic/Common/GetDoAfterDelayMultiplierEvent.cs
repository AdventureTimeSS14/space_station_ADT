namespace Content.Shared.ADT.Heretic.Common;

// ADT: перенесено из Content.Shared._Shitmed.DoAfter (без релея по частям тела)

public sealed class GetDoAfterDelayMultiplierEvent(float multiplier = 1f) : EntityEventArgs
{
    public float Multiplier = multiplier;
}
