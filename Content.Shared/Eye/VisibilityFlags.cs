using Robust.Shared.Serialization;

namespace Content.Shared.Eye
{
    [Flags]
    [FlagsFor(typeof(VisibilityMaskLayer))]
    public enum VisibilityFlags : int
    {
        None = 0,
        Normal = 1 << 0,
        Ghost = 1 << 1,
        Subfloor = 1 << 2,
        PhantomVessel = 1 << 3, // ADT Phantom
        Narcotic = 1 << 4, // ADT-Changeling-Tweak
        Schizo = 1 << 5, // ADT-Changeling-Tweak
        LingToxin = 1 << 6, // ADT-Changeling-Tweak
        EldritchInfluence = 1 << 7, // ADT-Heretic (Goobstation)
        EldritchInfluenceSpent = 1 << 8, // ADT-Heretic (Goobstation)
        Bubblegum = 1 << 9, // ADT-Tweak Bubblegum
        HereticCarving = 1 << 10, // ADT-Heretic (Goobstation)
    }
}
