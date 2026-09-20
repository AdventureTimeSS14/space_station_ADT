using Content.Shared.NPC.Prototypes;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Lavaland;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ADTNecropolisGateComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Open;

    [DataField, AutoNetworkedField]
    public bool Locked;

    [DataField]
    public ProtoId<NpcFactionPrototype>? UnlockFaction;

    [DataField]
    public TimeSpan OpenDelay = TimeSpan.FromSeconds(2.2);

    [DataField]
    public TimeSpan CloseDelay = TimeSpan.FromSeconds(0.5);

    [DataField]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/Effects/stonedoor_openclose.ogg");
}

[Serializable, NetSerializable]
public enum ADTNecropolisGateVisuals : byte
{
    Open,
}
