using Robust.Shared.Serialization;

namespace Content.Shared.Eye
{
    [Flags]
    [FlagsFor(typeof(VisibilityMaskLayer))]
    public enum VisibilityFlags : int
    {
        None   = 0,
        Normal = 1 << 0,
        Ghost  = 1 << 1,   // ADT Phantom
        PhantomVessel = 2 << 1, // ADT Phantom
        Narcotic = 1 << 2, // ADT-Changeling-Tweak
        Schizo = 1 << 3, // ADT-Changeling-Tweak
        LingToxin = 1 << 4, // ADT-Changeling-Tweak
    }
}
