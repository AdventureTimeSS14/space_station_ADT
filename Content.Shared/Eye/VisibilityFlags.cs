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
        Narcotic = 1 << 3,
        Schizo = 1 << 4,
        LingToxin = 1 << 5,
        Eldritch = 1 << 6,
    }
}
