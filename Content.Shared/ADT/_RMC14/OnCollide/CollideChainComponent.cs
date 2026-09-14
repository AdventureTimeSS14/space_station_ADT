// Ported from RMC-14 (https://github.com/RMC-14/RMC-14), MIT License

namespace Content.Shared._RMC14.OnCollide;

[RegisterComponent]
public sealed partial class CollideChainComponent : Component
{
    [ViewVariables]
    public HashSet<EntityUid> Hit = new();
}
