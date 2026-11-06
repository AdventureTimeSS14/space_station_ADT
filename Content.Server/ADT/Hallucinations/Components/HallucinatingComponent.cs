using Content.Server.ADT.Hallucinations.Types;

namespace Content.Server.ADT.Hallucinations.Components;

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
    public Dictionary<string, HashSet<HallucinationCompound>> Hallucinations = new();

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

    public sealed class HallucinationCompound
    {
        public BaseHallucinationsType Type;
        public TimeSpan PerformTime;

        public HallucinationCompound(BaseHallucinationsType type, TimeSpan performTime)
        {
            Type = type;
            PerformTime = performTime;
        }
    }
}
