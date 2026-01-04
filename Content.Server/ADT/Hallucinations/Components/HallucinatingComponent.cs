namespace Content.Server.ADT.Shizophrenia;

/// <summary>
/// Component added to currently hallucinating entities
/// </summary>
[RegisterComponent]
public sealed partial class HallucinatingComponent : Component
{
    /// <summary>
    /// Current hallucinations with their ids
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public Dictionary<string, BaseHallucinationsEntry?> Hallucinations = new();

    /// <summary>
    /// Lifetimes for temporal hallucinations
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public Dictionary<string, TimeSpan> Removes = new();

    /// <summary>
    /// Hallucinations music
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public List<string> Music = new();

    public TimeSpan NextUpdate = TimeSpan.Zero;
}
