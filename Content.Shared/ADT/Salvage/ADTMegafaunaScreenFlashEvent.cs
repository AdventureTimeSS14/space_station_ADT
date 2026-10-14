using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Salvage;

[Serializable, NetSerializable]
public sealed class ADTMegafaunaScreenFlashEvent : EntityEventArgs
{
    public Color Color;

    public float Duration;

    public ADTMegafaunaScreenFlashEvent(Color color, float duration)
    {
        Color = color;
        Duration = duration;
    }
}
