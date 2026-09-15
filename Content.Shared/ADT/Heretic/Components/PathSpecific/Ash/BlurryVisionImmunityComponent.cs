//

using Content.Shared.StatusEffect;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Heretic.Components.PathSpecific.Ash;

[RegisterComponent, NetworkedComponent]
public sealed partial class BlurryVisionImmunityComponent : Component
{
    [DataField]
    public ProtoId<StatusEffectPrototype> Key = "BlurryVision";
}
