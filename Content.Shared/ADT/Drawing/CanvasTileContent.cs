using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Drawing;

[Serializable, NetSerializable]
public enum CanvasTileKind : byte
{
    Empty,
    Solid,
    Blob,
}

[Serializable, NetSerializable]
public readonly record struct CanvasTileContent(CanvasTileKind Kind, uint Rgba, CanvasHash Hash)
{
    public static readonly CanvasTileContent Empty = new(CanvasTileKind.Empty, 0, default);

    public static CanvasTileContent Solid(uint rgba)
    {
        return new CanvasTileContent(CanvasTileKind.Solid, rgba, default);
    }

    public static CanvasTileContent Blob(CanvasHash hash)
    {
        return new CanvasTileContent(CanvasTileKind.Blob, 0, hash);
    }
}
