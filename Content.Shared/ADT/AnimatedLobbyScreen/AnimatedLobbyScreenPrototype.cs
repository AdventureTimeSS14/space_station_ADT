using Robust.Shared.Prototypes;

namespace Content.Shared.ADT;

/// <summary>
/// This is a prototype for...
/// </summary>
[Prototype]
public sealed partial class AnimatedLobbyScreenPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; } = default!;

    [DataField(required: true)]
    public string Path = default!;
}