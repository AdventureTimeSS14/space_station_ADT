using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Crushers.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class ADTCrusherRageOverlayStatusEffectComponent : Component
{
    [DataField]
    public Color Color = Color.FromHex("#C80000");

    [DataField]
    public float MinAmount = 0.15f;

    [DataField]
    public float MaxAmount = 0.3f;

    [DataField]
    public float PulseFrequency = 1.5f;

    [DataField]
    public float FlashAmount = 0.6f;

    [DataField]
    public TimeSpan FlashDuration = TimeSpan.FromSeconds(0.4);
}
