using Content.Server.NPC.HTN;
using Content.Shared.NPC.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.NPC;

[RegisterComponent]
public sealed partial class ChangeFactionStatusEffectComponent : Component
{
    [DataField]
    public ProtoId<NpcFactionPrototype>? NewFaction;

    [ViewVariables(VVAccess.ReadOnly)]
    public HashSet<ProtoId<NpcFactionPrototype>> OldFactions = new();

    [ViewVariables(VVAccess.ReadOnly)]
    public HTNCompoundTask? OriginalRootTask;

    [ViewVariables(VVAccess.ReadOnly)]
    public bool WasGivenHTN;

    [ViewVariables(VVAccess.ReadOnly)]
    public bool WasGivenCombatMode;
}
