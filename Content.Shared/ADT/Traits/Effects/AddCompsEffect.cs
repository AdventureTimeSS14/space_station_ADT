using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations;

namespace Content.Shared.ADT.Traits.Effects;

/// <summary>
/// Effect that adds components to the player entity.
/// Components are added without overwriting existing ones.
/// </summary>
public sealed partial class AddCompsEffect : BaseTraitEffect
{
    /// <summary>
    /// The components to add to the entity.
    /// </summary>
    [DataField(required: true, customTypeSerializer: typeof(ComponentRegistrySerializer))]
    public ComponentRegistry Components = new();

    public override void Apply(TraitEffectContext ctx)
    {
        ctx.EntMan.AddComponents(ctx.Player, Components, removeExisting: false);
    }
}
