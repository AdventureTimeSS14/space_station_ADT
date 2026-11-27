using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Heretic.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class RaggedSwordComponent : Component
{
    [DataField]
    public EntProtoId StatusEffect = "StatusEffectRaggedSwordRegen";

    [ViewVariables]
    public EntityUid? User;
}