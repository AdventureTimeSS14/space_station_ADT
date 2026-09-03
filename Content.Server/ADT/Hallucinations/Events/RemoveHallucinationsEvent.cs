namespace Content.Server.ADT.Hallucinations.Events;

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
