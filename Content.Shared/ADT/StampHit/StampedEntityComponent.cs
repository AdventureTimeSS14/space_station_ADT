namespace Content.Shared.ADT.StampHit;

[RegisterComponent]
public sealed partial class StampedEntityComponent : Component
{
    /// <summary>
    /// The stamps, that on entity
    /// </summary>
    [DataField]
    public List<string> StampToEntity = [];
}
