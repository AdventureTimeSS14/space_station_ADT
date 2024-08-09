using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Content.Shared.DoAfter;
using Robust.Shared.Prototypes;
using Robust.Shared.Audio;
using Robust.Shared.Random;

namespace Content.Shared.StatusEffect;

[RegisterComponent]
public sealed partial class HungerEffectComponent : Component
{
    [DataField]
    public float Multiplier = 5f;
}
