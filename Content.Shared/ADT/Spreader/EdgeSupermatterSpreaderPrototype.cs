using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Spreader;

/// <summary>
/// Adds this node group to <see cref="Content.Server.ADT.Spreader.SupermatterKudzuSystem"/> for tick updates.
/// </summary>
[Prototype("edgeSupermatterSpreader")]
public sealed partial class EdgeSupermatterSpreaderPrototype : IPrototype
{
    [IdDataField] public string ID { get; } = string.Empty;
    [DataField(required:true)] public int UpdatesPerSecond;

    /// <summary>
    /// If true, this spreader can't spread onto spaced tiles like lattice.
    /// </summary>
    [DataField]
    public bool PreventSpreadOnSpaced = true;
}