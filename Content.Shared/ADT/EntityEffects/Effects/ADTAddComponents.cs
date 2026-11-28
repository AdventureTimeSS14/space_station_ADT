using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.EntityEffects.Effects;

public sealed partial class ADTAddComponents : EntityEffectBase<ADTAddComponents>
{
    [DataField(required: true)]
    public ComponentRegistry Components = new();

    [DataField]
    public LocId? Popup;
}
