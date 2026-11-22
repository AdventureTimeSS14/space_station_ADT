namespace Content.Server.ADT.Procedural;

[RegisterComponent]
public sealed partial class ADTOccupiedRoomsComponent : Component
{
    [ViewVariables]
    public List<Box2> Rooms = new();
}
