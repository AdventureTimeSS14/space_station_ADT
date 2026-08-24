// Ported from RMC-14 (https://github.com/RMC-14/RMC-14), MIT License

using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.OnCollide;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CollideChainComponent : Component
{
    [DataField, AutoNetworkedField]
    public HashSet<EntityUid> Hit = new();
}
