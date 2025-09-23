using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.Heretic;
using Content.Shared.Popups;
using Robust.Shared.Audio;
using Content.Server.Humanoid;
using Content.Shared.ActionBlocker;
using Content.Shared.Actions;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Polymorph.Systems;
using Content.Shared.Standing;
using Content.Shared.Tools.Systems;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;
using System.Linq;

namespace Content.Server.Heretic.Abilities;

public sealed partial class HereticAbilitySystem : EntitySystem
{
    public ProtoId<DamageGroupPrototype> BruteDamageGroup = "Brute";
    public ProtoId<DamageGroupPrototype> BurnDamageGroup = "Burn";
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedChameleonProjectorSystem _chameleon = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly HungerSystem _hunger = default!;
    [Dependency] private readonly HumanoidAppearanceSystem _humanoid = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] private readonly MobThresholdSystem _threshold = default!;
    [Dependency] private readonly WeldableSystem _weldable = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;
    private void SubscribeFlesh()
    {
        SubscribeLocalEvent<HereticComponent, EventHereticFleshSurgery>(OnFleshSurgery);
        SubscribeLocalEvent<HereticComponent, EventHereticFleshSurgeryDoAfter>(OnFleshSurgeryDoAfter);
        SubscribeLocalEvent<HereticComponent, HereticAscensionFleshEvent>(OnFleshAscendPolymorph);
    }

    private void OnFleshSurgery(Entity<HereticComponent> ent, ref EventHereticFleshSurgery args)
    {
        if (!TryUseAbility(ent, args))
            return;

        if (HasComp<GhoulComponent>(args.Target)
        || (TryComp<HereticComponent>(args.Target, out var th) && th.CurrentPath == ent.Comp.CurrentPath))
        {
            var dargs = new DoAfterArgs(EntityManager, ent, 10f, new EventHereticFleshSurgeryDoAfter(args.Target), ent, args.Target)
            {
                BreakOnDamage = true,
                BreakOnMove = true,
                BreakOnHandChange = false,
                BreakOnDropItem = false,
            };
            _doafter.TryStartDoAfter(dargs);
            args.Handled = true;
            return;
        }

        if (TryComp<DamageableComponent>(args.Target, out var dmgComp))
        {
            _vomit.Vomit(args.Target, -1000, -1000); // You feel hollow!
            var damage = _random.NextFloat(20, 99);
            var damage_brute = new DamageSpecifier(_prot.Index(BruteDamageGroup), -damage);
            var damage_burn = new DamageSpecifier(_prot.Index(BurnDamageGroup), -damage);
            _damageable.TryChangeDamage(ent, damage_brute);
            _damageable.TryChangeDamage(ent, damage_burn);

            damage = (float)(dmgComp.TotalDamage + damage) / _prot.EnumeratePrototypes<DamageTypePrototype>().Count();

            _dmg.SetAllDamage(args.Target, dmgComp, damage);
            args.Handled = true;
        }
    }
    private void OnFleshSurgeryDoAfter(Entity<HereticComponent> ent, ref EventHereticFleshSurgeryDoAfter args)
    {
        if (args.Cancelled)
            return;

        if (args.Target == null) // shouldn't really happen. just in case
            return;

        if (!TryComp<DamageableComponent>(args.Target, out var dmg))
            return;

        // heal teammates, mostly ghouls
        _dmg.SetAllDamage((EntityUid)args.Target, dmg, 0);
        args.Handled = true;
    }
    private void OnFleshAscendPolymorph(Entity<HereticComponent> ent, ref HereticAscensionFleshEvent args)
    {
        var urist = _poly.PolymorphEntity(ent, "EldritchHorror");
        if (urist == null)
            return;

        _aud.PlayPvs(new SoundPathSpecifier("/Audio/Animals/space_dragon_roar.ogg"), (EntityUid)urist, AudioParams.Default.AddVolume(2f));
    }
}
