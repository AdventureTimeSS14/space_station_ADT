using Content.Shared.Damage.Prototypes;
using Content.Shared.Stacks;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Lavaland.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class ADTGoliathPlatableComponent : Component
{
    [DataField]
    public ProtoId<StackPrototype> Stack = "GoliathHide";

    [DataField]
    public List<ProtoId<DamageTypePrototype>> Types = new() { "Blunt", "Slash", "Piercing" };

    [DataField]
    public float Step = 0.1f;

    [DataField]
    public float MinCoefficient = 0.4f;

    [DataField]
    public int MaxPlates;

    [DataField, AutoNetworkedField]
    public int Plates;

    [DataField]
    public List<string> Sprites = new();
}
