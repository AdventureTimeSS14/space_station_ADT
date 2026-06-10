using System.Linq;
using Robust.Shared.Prototypes;
using Robust.Shared.Audio.Systems;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Standing;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Popups;
using Content.Shared.IdentityManagement;
using Content.Shared.ADT.Grab;
using Content.Shared.ADT.MartialArts;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Damage.Systems;

using Content.Shared.Weapons.Melee.Events;

namespace Content.Shared.ADT.Combat;

public abstract class SharedNecksnapSystem : EntitySystem
{
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly PullingSystem _pullingSystem = default!;

    private static readonly ProtoId<DamageTypePrototype> BluntDamageType = "Blunt";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<NecksnapComponent, MeleeHitEvent>(OnMeleeHit);
    }

    private void OnMeleeHit(EntityUid uid, NecksnapComponent comp, MeleeHitEvent args)
    {
        if (!args.IsHit || !args.HitEntities.Any())
            return;

        var user = args.User;
        var target = args.HitEntities[0];

        if (_standing.IsDown(target) &&
             !_mobState.IsDead(target) &&
             !HasComp<GodmodeComponent>(target) &&
             TryComp<PullerComponent>(user, out var puller) &&
             TryComp<GrabIntentComponent>(user, out var grabIntent) &&
             grabIntent.GrabStage == GrabStage.Suffocate &&
             puller.Pulling == target &&
             _mobThreshold.TryGetDeadThreshold(target, out var damageToKill) &&
             damageToKill != null
             && TryComp(target, out StaminaComponent? stamina) && stamina.Critical) // проверка условий для перелома шеи
        {
            if (TryComp<CanPerformComboComponent>(user, out var combo))
                combo.LastAttacks.Clear();
            var blunt = new DamageSpecifier(_proto.Index(BluntDamageType), damageToKill.Value);
            _damageable.TryChangeDamage(target, blunt, true);
            _audio.PlayPvs(comp.Sound, target);
            if (comp.Popup != null)
                _popup.PopupPredicted(Loc.GetString(comp.Popup, ("user", Identity.Entity(user, _entManager)), ("target", target)), target, target, PopupType.LargeCaution);
            if (TryComp<PullableComponent>(target, out var pulled))
            {
                _pullingSystem.TryStopPull(target, pulled, user); // освобождаем от граба гнилой труп
            }
        }
    }

}
