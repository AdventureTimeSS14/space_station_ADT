//

using Content.Shared.Heretic.Components.PathSpecific.Ash;
using Content.Shared.StatusEffectNew;

namespace Content.Shared.ADT.Heretic.Systems.PathSpecific.Ash;

public sealed partial class BlindnessImmunitySystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BlurryVisionImmunityComponent, BeforeStatusEffectAddedEvent>(OnBeforeBlur);
        SubscribeLocalEvent<BlindnessImmunityComponent, BeforeStatusEffectAddedEvent>(OnBeforeBlindness);
    }

    private void OnBeforeBlur(Entity<BlurryVisionImmunityComponent> ent, ref BeforeStatusEffectAddedEvent args)
    {
        if (args.Effect.Id == ent.Comp.Key.Id)
            args.Cancelled = true;
    }

    private void OnBeforeBlindness(Entity<BlindnessImmunityComponent> ent, ref BeforeStatusEffectAddedEvent args)
    {
        if (args.Effect == ent.Comp.Key)
            args.Cancelled = true;
    }
}
