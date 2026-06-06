using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Content.Shared.Audio.Jukebox;

namespace Content.Shared.ADT.Audio.Jukebox;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class JukeboxDiskComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<ProtoId<JukeboxPrototype>>? Tracks;

    [DataField]
    public ProtoId<JukeboxListPrototype>? TracksCollection = null;

    [DataField]
    public bool UseRandom = false;

    [DataField]
    public int RandomTracksMin = 3;

    [DataField]
    public int RandomTracksMax = 8;
}
