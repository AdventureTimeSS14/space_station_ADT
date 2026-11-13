using System.Linq;
using Content.Shared.Armor;
using Content.Shared.ADT.Body.Bones;
using Content.Shared.Body;
using Content.Shared.Buckle;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.Gravity;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.ADT.Body.Bones;

public sealed partial class ADTFractureSystem
{
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private StandingStateSystem _standing = default!;
    [Dependency] private SharedBuckleSystem _buckle = default!;
    [Dependency] private SharedGravitySystem _gravity = default!;
    [Dependency] private SharedStunSystem _stun = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;

    private const float MaxStepDistance = 2f;
    private const float PainMessageChance = 0.03f;
    private static readonly TimeSpan FallKnockdown = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan ReattachDelay = TimeSpan.FromSeconds(10);

    private void InitializeEffects()
    {
        SubscribeLocalEvent<ADTFracturedComponent, MoveEvent>(OnMove);
        SubscribeLocalEvent<ADTFracturedComponent, MeleeAttackEvent>(OnMeleeAttack);
        SubscribeLocalEvent<ADTFracturedComponent, ADTBoneReattachDoAfterEvent>(OnReattachDoAfter);
        SubscribeLocalEvent<GunComponent, GunShotEvent>(OnGunShot);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = Timing.CurTime;
        var query = EntityQueryEnumerator<ADTFracturedComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.IgnoreUntil == TimeSpan.Zero || comp.IgnoreUntil > now)
                continue;

            SetIgnoreUntil((uid, comp), TimeSpan.Zero);
            Refresh(uid);
        }
    }

    public DamageSpecifier ApplyZoneArmor(EntityUid body, ProtoId<OrganCategoryPrototype> zone, DamageSpecifier damage)
    {
        if (!TryComp<ADTBonesComponent>(body, out var bones) || !bones.Coverage.TryGetValue(zone, out var slots))
            return damage;

        if (!_inventory.TryGetContainerSlotEnumerator(body, out var enumerator, slots))
            return damage;

        while (enumerator.NextItem(out var item))
        {
            if (TryComp<ArmorComponent>(item, out var armor))
                damage = DamageSpecifier.ApplyModifierSet(damage, armor.Modifiers);
        }

        return damage;
    }

    private void OnMove(Entity<ADTFracturedComponent> ent, ref MoveEvent args)
    {
        if (args.ParentChanged || args.OnlyRotation)
            return;

        var distance = (args.NewPosition.Position - args.OldPosition.Position).Length();
        if (distance > MaxStepDistance)
            return;

        if (_standing.IsDown(ent.Owner) || _buckle.IsBuckled(ent) || _gravity.IsWeightless(ent.Owner))
            return;

        ent.Comp.Distance += distance;
        while (ent.Comp.Distance >= 1f)
        {
            ent.Comp.Distance -= 1f;
            if (!OnStep(ent))
                return;
        }
    }

    private bool OnStep(Entity<ADTFracturedComponent> ent)
    {
        if (ent.Comp.HasSplints && WearSplints(ent))
            return false;

        if (!ent.Comp.HasFractures || IsIgnoring(ent.Comp))
            return true;

        if (ent.Comp.FallChance > 0f && _random.Prob(ent.Comp.FallChance))
        {
            Fall(ent);
            return false;
        }

        if (_random.Prob(PainMessageChance) && CanFeelPain(ent))
            _popup.PopupEntity(Loc.GetString("adt-fracture-moving-pain"), ent, ent, PopupType.SmallCaution);

        return true;
    }

    private bool WearSplints(Entity<ADTFracturedComponent> ent)
    {
        var broke = false;
        foreach (var (bone, _) in EnumerateBones(ent).ToList())
        {
            if (!bone.Comp.Splinted)
                continue;

            bone.Comp.SplintDistance -= 1f;
            if (bone.Comp.SplintDistance > 0f)
                continue;

            RemoveSplint(ent, bone, true);
            broke = true;
        }

        return broke;
    }

    private void Fall(Entity<ADTFracturedComponent> ent)
    {
        var damage = new DamageSpecifier();
        foreach (var (bone, category) in EnumerateBones(ent))
        {
            if (!IsLeg(category) || bone.Comp.Splinted || GetFractureType(bone.Comp) is not { } type)
                continue;

            damage += type.FallDamage;
        }

        _stun.TryKnockdown(ent.Owner, FallKnockdown, true);
        if (!damage.Empty)
            _damageable.TryChangeDamage(ent.Owner, damage, interruptsDoAfters: false);

        _popup.PopupEntity(Loc.GetString("adt-fracture-fall-self"), ent, ent, PopupType.MediumCaution);
        _popup.PopupEntity(Loc.GetString("adt-fracture-fall-others", ("target", ent.Owner)), ent, Filter.PvsExcept(ent), true, PopupType.Medium);
        TryScream(ent);
    }

    private void OnMeleeAttack(Entity<ADTFracturedComponent> ent, ref MeleeAttackEvent args)
    {
        if (args.Weapon != ent.Owner)
            TryDropFromBrokenHand(ent, args.Weapon);
    }

    private void OnGunShot(Entity<GunComponent> ent, ref GunShotEvent args)
    {
        if (TryComp<ADTFracturedComponent>(args.User, out var fractured))
            TryDropFromBrokenHand((args.User, fractured), ent);
    }

    private void TryDropFromBrokenHand(Entity<ADTFracturedComponent> ent, EntityUid item)
    {
        if (!ent.Comp.HasFractures || IsIgnoring(ent.Comp) || !TryGetHandSide(ent, item, out var left))
            return;

        var chance = left ? ent.Comp.DropLeft : ent.Comp.DropRight;
        if (chance <= 0f || !_random.Prob(chance))
            return;

        if (!_hands.TryDrop(ent.Owner, item, checkActionBlocker: false))
            return;

        _popup.PopupEntity(Loc.GetString("adt-fracture-drop-self", ("item", item)), ent, ent, PopupType.MediumCaution);
        _popup.PopupEntity(Loc.GetString("adt-fracture-drop-others", ("target", ent.Owner), ("item", item)), ent, Filter.PvsExcept(ent), true, PopupType.Small);
        TryScream(ent);
    }

    public void StartReattach(EntityUid user, EntityUid body, EntityUid organ)
    {
        var args = new DoAfterArgs(EntityManager, user, ReattachDelay, new ADTBoneReattachDoAfterEvent(GetNetEntity(organ)), body, body)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
        };

        if (!_doAfter.TryStartDoAfter(args))
            return;

        _popup.PopupEntity(Loc.GetString("adt-fracture-reattach-start", ("user", user), ("limb", organ)), body, PopupType.Medium);
    }

    private void OnReattachDoAfter(Entity<ADTFracturedComponent> ent, ref ADTBoneReattachDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;

        var organ = GetEntity(args.Organ);
        if (!TryComp<ADTBoneComponent>(organ, out var bone)
            || CompOrNull<OrganComponent>(organ)?.Body != ent.Owner
            || GetFractureType(bone) is not { Open: true, ReattachInto: { } into } type)
            return;

        TryScream(ent);

        if (_random.Prob(type.ReattachChance))
        {
            Downgrade(ent, (organ, bone), into);
            _popup.PopupEntity(Loc.GetString("adt-fracture-reattach-success", ("limb", organ)), ent, args.User, PopupType.Medium);
            return;
        }

        _damageable.TryChangeDamage(ent.Owner, type.ReattachFailDamage, interruptsDoAfters: false, origin: args.User);
        _popup.PopupEntity(Loc.GetString("adt-fracture-reattach-fail", ("limb", organ)), ent, args.User, PopupType.MediumCaution);
    }
}
