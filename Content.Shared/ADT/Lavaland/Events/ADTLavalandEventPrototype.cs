using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Lavaland.Events;

[Prototype("lavalandEvent")]
public sealed partial class ADTLavalandEventPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public LocId? Announcement;

    [DataField]
    public SoundSpecifier? Sound;

    [DataField(required: true)]
    public List<ADTLavalandEventEffect> Effects = new();

    [DataField]
    public float Weight = 1f;
}
