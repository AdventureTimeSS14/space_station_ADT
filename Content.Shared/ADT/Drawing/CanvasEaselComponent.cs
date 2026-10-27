using System.Numerics;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Drawing;

[RegisterComponent]
public sealed partial class CanvasEaselComponent : Component
{
    [DataField]
    public string SlotId = "canvas";

    [ViewVariables]
    public Guid? Session;
}

[RegisterComponent]
public sealed partial class CanvasItemComponent : Component
{
    [DataField(required: true)]
    public Vector2i Size;

    [DataField(required: true)]
    public string EaselState = string.Empty;

    [DataField]
    public Vector2 EaselOffset;

    [ViewVariables]
    public Guid? PaintingUid;

    [ViewVariables]
    public NetUserId? Owner;
}

[Serializable, NetSerializable]
public enum CanvasEaselUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public enum CanvasEaselVisuals : byte
{
    Canvas,
    Offset,
    Painting,
    PaintingSize,
    PaintingOffset,
}

[Serializable, NetSerializable]
public enum CanvasItemVisuals : byte
{
    Painted,
}
