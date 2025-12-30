using Content.Shared.Destructible.Thresholds;
using Content.Shared.Popups;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Shizophrenia;

[Prototype]
public sealed partial class HallucinationsPackPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; } = default!;

    [DataField]
    public BaseHallucinationsType? Data;

    [DataField]
    public ComponentRegistry Components = new();

    [DataField]
    public string? StartingMessage;

    [DataField]
    public PopupType MessageType = PopupType.MediumCaution;

    [DataField]
    public SoundSpecifier? Music;

    [DataField]
    public float MusicDurationThreshold = 1f;

    [DataField]
    public MinMax? MusicPlayInterval;
}
