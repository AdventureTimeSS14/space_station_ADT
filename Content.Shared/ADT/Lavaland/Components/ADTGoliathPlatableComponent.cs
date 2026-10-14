using Content.Shared.Damage.Prototypes;
using Content.Shared.Stacks;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Lavaland.Components;

[RegisterComponent, NetworkedComponent]
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
    public TimeSpan Delay = TimeSpan.FromSeconds(2);

    [DataField]
    public EntProtoId? Upgrade;
}
