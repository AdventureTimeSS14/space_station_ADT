using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
namespace Content.Shared.ADT.Heretic.Components.Side;
[RegisterComponent, NetworkedComponent]
public sealed partial class HereticPaintingComponent : Component
{
    [DataField]
    public HereticPaintingKind Kind = HereticPaintingKind.Weeping;

    public HashSet<NetEntity> ValidTargets = new();
}
[Serializable, NetSerializable]
public enum HereticPaintingKind : byte
{
    Weeping,
    Desire,
    Vines,
    Beauty,
    Rust
}
