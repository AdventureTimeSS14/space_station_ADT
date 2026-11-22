using Content.Shared.ADT.Vampire;
using Content.Shared.ADT.Vampire.Components;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects;
using Content.Shared.Jittering;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Robust.Shared.Timing;

namespace Content.Server.ADT.EntityEffects;

public sealed partial class ADTVampireGarlicEffectSystem : EntityEffectSystem<VampireComponent, ADTVampireGarlic>
{
    [Dependency] private SharedStunSystem _stun = default!;
    [Dependency] private SharedJitteringSystem _jitter = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    protected override void Effect(Entity<VampireComponent> entity, ref EntityEffectEvent<ADTVampireGarlic> args)
    {
        if (entity.Comp.Variant == VampireVariant.Classic || HasComp<SupremeVampireComponent>(entity))
            return;

        _stun.TryKnockdown(entity.Owner, args.Effect.KnockdownTime);
        _jitter.AddJitter(entity.Owner);
        Timer.Spawn(args.Effect.JitterTime, () => RemCompDeferred<JitteringComponent>(entity.Owner));
        _popup.PopupEntity(Loc.GetString("vampire-garlic-weakened"), entity, entity, PopupType.MediumCaution);
    }
}