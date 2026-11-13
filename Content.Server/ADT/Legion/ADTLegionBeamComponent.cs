namespace Content.Server.ADT.Legion;

[RegisterComponent]
public sealed partial class ADTLegionBeamComponent : Component
{
    [ViewVariables]
    public EntityUid Caster;

    [ViewVariables]
    public EntityUid Grid;

    [ViewVariables]
    public List<Vector2i> Tiles = new();

    [ViewVariables]
    public HashSet<EntityUid> Inside = new();

    [ViewVariables]
    public TimeSpan EndsAt;

    [ViewVariables]
    public TimeSpan NextTick;
}
