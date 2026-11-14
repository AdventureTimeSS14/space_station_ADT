using Content.Shared.StatusIcon;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Hallucinations.Components;

/// <summary>
/// Component added to entities experiencing hallucinations
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SchizophreniaComponent : Component
{
    /// <summary>
    /// List of hallucination entities
    /// Server-only
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public List<EntityUid> Hallucinations = new();

    /// <summary>
    /// Unique index for component owner and their hallucinations
    /// Used for sentinent hallucinations to identify owner
    /// </summary>
    [AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public int Idx = 0;

    [AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<FactionIconPrototype> FactionIcon = "Schizophrenic";
}
