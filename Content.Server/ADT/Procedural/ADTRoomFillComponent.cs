using Content.Shared.ADT.Procedural;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Procedural;

[RegisterComponent]
public sealed partial class ADTRoomFillComponent : Component
{
    [DataField]
    public ProtoId<ADTDungeonRoomPrototype>? Room;

    [DataField]
    public bool Rotation = true;

    [DataField]
    public Vector2i MinSize = new(0, 0);

    [DataField]
    public Vector2i MaxSize = new(100, 100);

    [DataField]
    public EntityWhitelist? RoomWhitelist;

    [DataField]
    public bool ClearExisting = true;
}
