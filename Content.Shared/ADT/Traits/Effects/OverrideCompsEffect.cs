using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations;

namespace Content.Shared.ADT.Traits.Effects;

/// <summary>
/// Effect that overrides component fields on the player entity.
/// If the component exists, its fields are overwritten with the new values.
/// If it doesn't exist, the component is added.
/// </summary>
public sealed partial class OverrideCompsEffect : BaseTraitEffect
{
    /// <summary>
    /// The components to add/override on the entity.
    /// </summary>
    [DataField(required: true, customTypeSerializer: typeof(ComponentRegistrySerializer))]
    public ComponentRegistry Components = new();

    public override void Apply(TraitEffectContext ctx)
    {
        ctx.EntMan.AddComponents(ctx.Player, Components, removeExisting: true);
    }
}
