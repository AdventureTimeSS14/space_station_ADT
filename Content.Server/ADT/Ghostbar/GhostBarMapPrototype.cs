using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.ADT.Ghostbar;

/// <summary>
/// прототип самих гост баров
/// </summary>
[Prototype("ghostbarMap")]
public sealed partial class GhostBarMapPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// путь до карты
    /// </summary>
    [DataField("path", required: true)]
    public string Path = string.Empty;

    /// <summary>
    /// включает/выключает пацифизм
    /// </summary>
    [DataField("pacified")]
    public bool Pacified = false;
}
