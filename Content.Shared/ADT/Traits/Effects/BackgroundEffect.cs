using Content.Shared.Tag;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Traits.Effects;

/// <summary>
/// Effect that adds/replaces a background to the player entity.
/// </summary>
public sealed partial class BackgroundEffect : BaseTraitEffect
{
    /// <summary>
    /// The background of the entity.
    /// </summary>
    [DataField(required: true)]
    public string Background = string.Empty;

    public override void Apply(TraitEffectContext ctx)
    {
        if (!ctx.EntMan.EntitySysManager.TryGetEntitySystem(out TagSystem? tagSystem))
            return;

        var tag = new ProtoId<TagPrototype>(Background + "TraitBackground");
        tagSystem.TryAddTag(ctx.Player, tag);
    }
}
