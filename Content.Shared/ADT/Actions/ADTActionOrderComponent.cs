using System.Collections.Immutable;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Actions;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class ADTActionOrderComponent : Component
{
    [ViewVariables, AutoNetworkedField]
    public ImmutableArray<EntProtoId> Order = ImmutableArray<EntProtoId>.Empty;

    [ViewVariables, AutoNetworkedField]
    public ImmutableArray<EntProtoId> Removed = ImmutableArray<EntProtoId>.Empty;
}
