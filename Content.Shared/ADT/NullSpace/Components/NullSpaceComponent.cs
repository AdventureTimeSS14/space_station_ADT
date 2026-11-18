using Content.Shared.NPC.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.NullSpace.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class NullSpaceComponent : Component
{
    public List<ProtoId<NpcFactionPrototype>> SuppressedFactions = new();
}
