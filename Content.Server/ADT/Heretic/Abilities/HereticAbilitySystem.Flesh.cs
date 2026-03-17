using Content.Shared.Damage;
using Content.Shared.Heretic;
using Robust.Shared.Audio;
using Content.Server.Humanoid;
using Content.Shared.ActionBlocker;
using Content.Shared.Actions;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Polymorph.Systems;
using Content.Shared.Standing;
using Content.Shared.Tools.Systems;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;
using System.Linq;
using Content.Shared.Damage.Systems;
using Content.Shared.Damage.Components;

namespace Content.Server.Heretic.Abilities;

public sealed partial class HereticAbilitySystem : EntitySystem
{
    public ProtoId<DamageGroupPrototype> BruteDamageGroup = "Brute";
    public ProtoId<DamageGroupPrototype> BurnDamageGroup = "Burn";
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedChameleonProjectorSystem _chameleon = default!;
    [Dependency] private readonly HungerSystem _hunger = default!;
    [Dependency] private readonly HumanoidAppearanceSystem _humanoid = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] private readonly WeldableSystem _weldable = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;
    private void SubscribeFlesh()
    {
        SubscribeLocalEvent<HereticComponent, EventHereticFleshSurgery>(OnFleshSurgery);
        SubscribeLocalEvent<HereticComponent, EventHereticGhoulCall>(OnGhoulCall);
        SubscribeLocalEvent<HereticComponent, HereticAscensionFleshEvent>(OnFleshAscendPolymorph);
    }

    private void OnFleshSurgery(Entity<HereticComponent> ent, ref EventHereticFleshSurgery args)
    {
        if (args.Target == ent.Owner)
            return;

        if (!TryUseAbility(ent, args))
            return;

        if (HasComp<GhoulComponent>(args.Target)
        || (TryComp<HereticComponent>(args.Target, out var th) && th.CurrentPath == ent.Comp.CurrentPath))
        {
            if (TryComp<DamageableComponent>(args.Target, out var dmg))
            {
                _damageable.SetAllDamage(args.Target, 0);
            }
            args.Handled = true;
            return;
        }

        if (TryComp<DamageableComponent>(args.Target, out var dmgComp))
        {
            _vomit.Vomit(args.Target, -1000, -1000); // You feel hollow!
            var damage = _random.NextFloat(20, 99);
            var damage_brute = new DamageSpecifier(_prot.Index(BruteDamageGroup), -damage);
            var damage_burn = new DamageSpecifier(_prot.Index(BurnDamageGroup), -damage);
            _damageable.TryChangeDamage(ent.Owner, damage_brute);
            _damageable.TryChangeDamage(ent.Owner, damage_burn);

            damage = (float)(dmgComp.TotalDamage + damage) / _prot.EnumeratePrototypes<DamageTypePrototype>().Count();

            _damageable.SetAllDamage(args.Target, damage);
            args.Handled = true;
        }
    }
    private void OnFleshAscendPolymorph(Entity<HereticComponent> ent, ref HereticAscensionFleshEvent args)
    {
        var urist = _poly.PolymorphEntity(ent, "EldritchHorror");
        if (urist == null)
            return;

        _aud.PlayPvs(new SoundPathSpecifier("/Audio/Animals/space_dragon_roar.ogg"), (EntityUid)urist, AudioParams.Default.AddVolume(2f));
    }
}
