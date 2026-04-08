using Content.Shared.Body.Events;
<<<<<<< HEAD
=======
using Content.Shared.StatusEffectNew;
>>>>>>> upstreamwiz/master

namespace Content.Shared.Traits.Assorted;

public sealed class HemophiliaSystem : EntitySystem
{
    public override void Initialize()
    {
<<<<<<< HEAD
        SubscribeLocalEvent<HemophiliaComponent, BleedModifierEvent>(OnBleedModifier);
    }

    private void OnBleedModifier(Entity<HemophiliaComponent> ent, ref BleedModifierEvent args)
    {
        args.BleedReductionAmount *= ent.Comp.HemophiliaBleedReductionMultiplier;
=======
        SubscribeLocalEvent<HemophiliaStatusEffectComponent, StatusEffectRelayedEvent<BleedModifierEvent>>(OnBleedModifier);
    }

    private void OnBleedModifier(Entity<HemophiliaStatusEffectComponent> ent, ref StatusEffectRelayedEvent<BleedModifierEvent> args)
    {
        var ev = args.Args;
        ev.BleedReductionAmount *= ent.Comp.BleedReductionMultiplier;
        ev.BleedAmount *= ent.Comp.BleedAmountMultiplier;
        args.Args = ev;
>>>>>>> upstreamwiz/master
    }
}
