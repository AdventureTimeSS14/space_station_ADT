using System.Numerics;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Screamer;

[Serializable, NetSerializable]
public sealed class DoScreamerMessage : EntityEventArgs
{
    public string ProtoId = default!;
    public string? Sound;
    public Vector2 Offset;
    public float Alpha;
    public float Duration;
    public bool FadeIn;
    public bool FadeOut;

    public DoScreamerMessage(string protoId, string? sound, Vector2 offset, float alpha, float duration, bool fadeIn, bool fadeOut)
    {
        ProtoId = protoId;
        Sound = sound;
        Offset = offset;
        Alpha = alpha;
        Duration = duration;
        FadeIn = fadeIn;
        FadeOut = fadeOut;
    }
}
