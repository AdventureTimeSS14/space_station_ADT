using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Anchored.Components
{
    [Serializable, NetSerializable]
    public enum AnchoredVisuals
    {
        VisualState
    }

    [Serializable, NetSerializable]
    public enum AnchoredVisualState
    {
        Free = 0,
        Anchored,
    }
}
