using Robust.Shared.Serialization;

namespace Content.Shared.Eye
{
    [Flags]
    [FlagsFor(typeof(VisibilityMaskLayer))]
    public enum VisibilityFlags : int
    {
        None = 0,
        Normal = 1 << 0,
        Ghost  = 1 << 1,
        Subfloor = 1 << 2,
        Eldritch = 1 << 7, // ADT-Tweak Heretic
        Bubblegum = 1 << 8, // ADT-Tweak Bubblegum
        Hallucination = 1 << 10, // ADT-Tweak
    }
}
