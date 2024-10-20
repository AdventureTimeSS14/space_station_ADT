using Content.Shared.Changeling.Components;
using Content.Shared.Changeling;
using Content.Shared.Inventory;
using Content.Server.Hands.Systems;
using Robust.Shared.Prototypes;
using Content.Server.Body.Systems;
using Content.Shared.Popups;
using Content.Shared.IdentityManagement;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Content.Shared.DoAfter;
using Content.Shared.Humanoid;
using Content.Server.Fluids.EntitySystems;
using Robust.Shared.Containers;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage;
using Robust.Server.GameObjects;
using Content.Server.Resist;

namespace Content.Server.Changeling.EntitySystems;

public sealed partial class LingSlugSystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly BloodstreamSystem _bloodstreamSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly EntityManager _entityManager = default!;
    [Dependency] private readonly TransformSystem _transform = default!;

    private void InitializeLingAbilities()
    {
        SubscribeLocalEvent<LingSlugComponent, LingEggActionEvent>(OnLayEggs);
        SubscribeLocalEvent<LingSlugComponent, LingEggDoAfterEvent>(OnLayEggsDoAfter);
        SubscribeLocalEvent<LingSlugComponent, LingHatchActionEvent>(OnHatch);

    }

    private void OnLayEggs(EntityUid uid, LingSlugComponent component, LingEggActionEvent args)     /// TODO: Заменить на кладку яиц при ударе.
    {
        if (args.Handled)
            return;

        var target = args.Target;

        if (!HasComp<HumanoidAppearanceComponent>(target))
        {
            var selfMessage = Loc.GetString("changeling-dna-fail-nohuman", ("target", Identity.Entity(target, EntityManager)));
            _popup.PopupEntity(selfMessage, uid, uid);
            return;
        }

        if (!_mobState.IsIncapacitated(target)) // if target isn't crit or dead dont let absorb
        {
            var selfMessage = Loc.GetString("changeling-dna-fail-notdead", ("target", Identity.Entity(target, EntityManager)));
            _popup.PopupEntity(selfMessage, uid, uid);
            return;
        }

        if (HasComp<AbsorbedComponent>(target))
        {
            var selfMessage = Loc.GetString("changeling-dna-alreadyabsorbed", ("target", Identity.Entity(target, EntityManager)));
            _popup.PopupEntity(selfMessage, uid, uid);
            return;
        }

        if (_tagSystem.HasTag(target, "ChangelingBlacklist"))
        {
            var selfMessage = Loc.GetString("changeling-dna-sting-fail-nodna", ("target", Identity.Entity(target, EntityManager)));
            _popup.PopupEntity(selfMessage, uid, uid);
            return;
        }

        if (LayEggs(uid, target, component))
        {
            args.Handled = true;

            var selfMessage = Loc.GetString("changeling-eggs-self-start", ("target", Identity.Entity(target, EntityManager)));
            _popup.PopupEntity(selfMessage, uid, uid, PopupType.MediumCaution);
        }

    }

    public ProtoId<DamageGroupPrototype> GeneticDamageGroup = "Genetic";
    private void OnLayEggsDoAfter(EntityUid uid, LingSlugComponent component, LingEggDoAfterEvent args)
    {
        if (args.Handled || args.Args.Target == null)
            return;

        args.Handled = true;
        var target = args.Args.Target.Value;

        if (args.Cancelled || !_mobState.IsIncapacitated(target) || HasComp<AbsorbedComponent>(target))
        {
            var selfMessage = Loc.GetString("changeling-eggs-interrupted");
            _popup.PopupEntity(selfMessage, uid, uid);
            return;
        }

        else
        {
            var holderComp = EnsureComp<LingEggsHolderComponent>(target);

            holderComp.Stomach = ContainerSystem.EnsureContainer<Container>(target, "stomach");
            var damage_genetic = new DamageSpecifier(_proto.Index(GeneticDamageGroup), holderComp.DamageAmount);
            _damageableSystem.TryChangeDamage(target, damage_genetic);    /// To be sure that target is dead

            var lingComp = EnsureComp<ChangelingComponent>(target);
            var xform = Transform(target);
            var selfMessage = Loc.GetString("changeling-eggs-self-success", ("target", Identity.Entity(target, EntityManager)));
            _popup.PopupEntity(selfMessage, uid, uid, PopupType.MediumCaution);

            component.EggsLaid = true;
            component.EggLing = target;

            holderComp.ChangelingHatchAction = component.HatchAction;
            lingComp.AbsorbedDnaModifier = component.AbsorbedDnaModifier;

            _transform.SetParent(uid, xform.ParentUid);
            _transform.SetCoordinates(uid, xform.Coordinates);

            ContainerSystem.Insert(uid, holderComp.Stomach);
            RemComp<CanEscapeInventoryComponent>(uid);
            if (component.Spread == false)
                _action.AddAction(uid, ref holderComp.ChangelingHatchActionEntity, holderComp.ChangelingHatchAction);
            else
                _action.AddAction(target, ref holderComp.ChangelingHatchActionEntity, holderComp.ChangelingHatchAction);

            _action.RemoveAction(uid, component.LayEggsActionEntity);   /// Яйца откладываются только один раз

            return;
        }
    }
    //public ProtoId<DamageGroupPrototype> BruteDamageGroup = "Brute";

    private void OnHatch(EntityUid uid, LingSlugComponent component, LingHatchActionEvent args)       /// TODO: Сделать из акшона автоматическую систему!
    {
        if (args.Handled)
            return;

        if (!component.EggsReady && component.EggLing != null)
        {
            ///_mobState.ChangeMobState(uid, MobState.Critical);

            var othersMessage = Loc.GetString("changeling-egg-others", ("user", Identity.Entity(component.EggLing.Value, EntityManager)));
            _popup.PopupEntity(othersMessage, component.EggLing.Value, Filter.PvsExcept(uid), true, PopupType.MediumCaution);

            var selfMessage = Loc.GetString("changeling-egg-self");
            _popup.PopupEntity(selfMessage, uid, uid, PopupType.MediumCaution);

            component.EggsReady = !component.EggsReady;

            args.Handled = true;
        }

        else
        {
            //RemComp<LingEggsHolderComponent>(uid);

            if (SpawnLingMonkey(uid, component))
            {
                if (component.EggLing != null)
                {
                    var damage_brute = new DamageSpecifier(_proto.Index(BruteDamageGroup), component.GibDamage);
                    _damageableSystem.TryChangeDamage(component.EggLing.Value, damage_brute);
                    _damageableSystem.TryChangeDamage(args.Performer, damage_brute);

                    args.Handled = true;
                }
            }
        }
    }

}
