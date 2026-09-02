using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Shizophrenia;

/// <summary>
/// Removes hallucinations with specified key from entity
/// </summary>
[DataDefinition]
public sealed partial class RemoveHallucinationsEvent : EntityEventArgs
{
    /// <summary>
    /// Time to remove from pack duration
    /// </summary>
    [DataField]
    public float Time;
}
