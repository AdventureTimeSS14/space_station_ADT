using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Drawing;

[Serializable, NetSerializable]
public enum PaintingStatus : byte
{
    Draft,
    Submitted,
    Approved,
    Rejected,
    Hidden,
    Deleted,
    Expired,
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class PaintingComponent : Component
{
    [DataField, AutoNetworkedField]
    public Guid? PaintingUid;

    [DataField, AutoNetworkedField]
    public Vector2i Size;

    [DataField, AutoNetworkedField]
    public string Title = string.Empty;

    [DataField, AutoNetworkedField]
    public bool Hidden;
}

[RegisterComponent]
public sealed partial class PaintingItemVisualsComponent : Component
{
    [DataField]
    public Vector2 InnerSize = new(23, 23);

    [DataField]
    public Vector2 InnerOffset = new(-4, -3);
}

[RegisterComponent]
public sealed partial class PaintingFrameComponent : Component
{
    [DataField]
    public string SlotId = "painting";

    [DataField]
    public Vector2 InnerSize = new(22, 22);

    [DataField]
    public Vector2 InnerOffset = Vector2.Zero;

    [DataField]
    public bool Gallery;

    [DataField]
    public bool PinnedOnly;
}

[Serializable, NetSerializable]
public enum PaintingFrameVisuals : byte
{
    Painting,
    Hidden,
}
