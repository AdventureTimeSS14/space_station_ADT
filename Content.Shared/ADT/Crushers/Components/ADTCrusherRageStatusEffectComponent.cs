using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Crushers.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class ADTCrusherRageStatusEffectComponent : Component
{
    [DataField]
    public EntProtoId ExhaustionEffect = "ADTStatusEffectCrusherRageExhaustion";

    [DataField]
    public TimeSpan ExhaustionDuration = TimeSpan.FromSeconds(3);
}
