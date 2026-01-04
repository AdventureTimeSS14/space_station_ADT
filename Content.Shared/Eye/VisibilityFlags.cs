using Robust.Shared.Serialization;

namespace Content.Shared.Eye
{
    [Flags]
    [FlagsFor(typeof(VisibilityMaskLayer))]
    public enum VisibilityFlags : int
    {
        None = 0,
        Normal = 1 << 0,
        Ghost  = 1 << 1,   // ADT Phantom
        Subfloor = 1 << 2,
        PhantomVessel = 2 << 1, // ADT Phantom
        Hallucination = 1 << 10, // ADT-Tweak
    }
}
