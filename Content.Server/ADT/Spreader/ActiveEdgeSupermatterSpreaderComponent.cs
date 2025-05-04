namespace Content.Server.ADT.Spreader;

/// <summary>
/// Added to entities being considered for spreading via <see cref="SupermatterSpreaderSystem"/>.
/// This needs to be manually added and removed.
/// </summary>
[RegisterComponent]
public sealed partial class ActiveEdgeSupermatterSpreaderComponent : Component
{
}
