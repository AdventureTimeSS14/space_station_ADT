using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Audio.Jukebox;

[RegisterComponent, NetworkedComponent]
public sealed partial class JukeboxDiskComponent : Component
{
    [DataField(required: true)]
    public ProtoId<JukeboxListPrototype> Collection = string.Empty;
}
