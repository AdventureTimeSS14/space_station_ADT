using Content.Shared.Audio.Jukebox;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Audio.Jukebox;

[Prototype]
public sealed partial class JukeboxListPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = string.Empty;

    [DataField(required: true)]
    public List<ProtoId<JukeboxPrototype>> Jukeboxes = new();
}
