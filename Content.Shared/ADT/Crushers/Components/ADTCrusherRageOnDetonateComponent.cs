using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Crushers.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ADTCrusherRageOnDetonateComponent : Component
{
    [DataField, AutoNetworkedField]
    public int MaxStacks = 3;

    [DataField, AutoNetworkedField]
    public TimeSpan StackDecay = TimeSpan.FromSeconds(5);

    [DataField, AutoNetworkedField]
    public TimeSpan RageDuration = TimeSpan.FromSeconds(5);

    [DataField, AutoNetworkedField]
    public TimeSpan BackstabExtension = TimeSpan.FromSeconds(1);

    [DataField, AutoNetworkedField]
    public TimeSpan MaxRageDuration = TimeSpan.FromSeconds(8);

    [DataField, AutoNetworkedField]
    public float LeechMultiplier = 2f;

    [DataField]
    public EntProtoId RageEffect = "ADTStatusEffectCrusherRage";

    [DataField]
    public EntProtoId ExhaustionEffect = "ADTStatusEffectCrusherRageExhaustion";

    [DataField]
    public SoundSpecifier? RageSound;

    [ViewVariables]
    public int Stacks;

    [ViewVariables]
    public TimeSpan LastStack;

    [ViewVariables]
    public TimeSpan RageStart;
}
