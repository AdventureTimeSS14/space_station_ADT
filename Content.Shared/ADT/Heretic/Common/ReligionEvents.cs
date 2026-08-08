namespace Content.Shared.ADT.Heretic.Common;

// ADT: from Goob NullrodEvents.cs

public sealed class TouchSpellDenialRelayEvent : CancellableEntityEventArgs;

public sealed class BeforeCastTouchSpellEvent(EntityUid target, bool doEffects = true) : CancellableEntityEventArgs
{
    /// <summary>
    /// The target of the event, to check if they meet the requirements for casting.
    /// </summary>
    public EntityUid? Target = target;

    public bool DoEffects = doEffects;
}
