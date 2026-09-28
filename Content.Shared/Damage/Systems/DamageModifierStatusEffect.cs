<<<<<<< HEAD
using Content.Shared.Damage.Components;
=======
﻿using Content.Shared.Damage.Components;
>>>>>>> wizards-filtered
using Content.Shared.StatusEffectNew;

namespace Content.Shared.Damage.Systems;

<<<<<<< HEAD
public sealed class DamageModifierStatusEffectSystem : EntitySystem
=======
public sealed partial class DamageModifierStatusEffectSystem : EntitySystem
>>>>>>> wizards-filtered
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DamageModifierStatusEffectComponent, StatusEffectRelayedEvent<DamageModifyEvent>>(OnDamageModifyStatus);
    }

    private void OnDamageModifyStatus(Entity<DamageModifierStatusEffectComponent> status, ref StatusEffectRelayedEvent<DamageModifyEvent> args)
    {
        args.Args.Damage = DamageSpecifier.ApplyModifierSet(args.Args.Damage, status.Comp.Modifiers);
    }
<<<<<<< HEAD
}
=======
}
>>>>>>> wizards-filtered
