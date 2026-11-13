using System.Diagnostics.CodeAnalysis;
using Content.Shared.ADT.Body.Prosthesis;
using Content.Shared.ADT.Heretic.Common;
using Content.Shared.Body;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Mobs.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.ADT.Body.Bones;

public abstract class SharedADTBoneSystem : EntitySystem
{
    [Dependency] protected IGameTiming Timing = default!;
    [Dependency] protected IPrototypeManager Proto = default!;
    [Dependency] protected BodySystem Body = default!;
    [Dependency] protected RoboticPartSystem RoboticPart = default!;
    [Dependency] private MovementSpeedModifierSystem _movement = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedGunSystem _gun = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private INetManager _net = default!;

    public static readonly ProtoId<OrganCategoryPrototype> Head = "Head";
    public static readonly ProtoId<OrganCategoryPrototype> Torso = "Torso";
    public static readonly ProtoId<OrganCategoryPrototype> ArmLeft = "ArmLeft";
    public static readonly ProtoId<OrganCategoryPrototype> ArmRight = "ArmRight";
    public static readonly ProtoId<OrganCategoryPrototype> HandLeft = "HandLeft";
    public static readonly ProtoId<OrganCategoryPrototype> HandRight = "HandRight";
    public static readonly ProtoId<OrganCategoryPrototype> LegLeft = "LegLeft";
    public static readonly ProtoId<OrganCategoryPrototype> LegRight = "LegRight";
    public static readonly ProtoId<OrganCategoryPrototype> FootLeft = "FootLeft";
    public static readonly ProtoId<OrganCategoryPrototype> FootRight = "FootRight";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTBoneComponent, OrganGotInsertedEvent>(OnBoneInserted);
        SubscribeLocalEvent<ADTBoneComponent, OrganGotRemovedEvent>(OnBoneRemoved);

        SubscribeLocalEvent<ADTFracturedComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshSpeed);
        SubscribeLocalEvent<ADTFracturedComponent, GetDoAfterDelayMultiplierEvent>(OnDoAfterDelay);
        SubscribeLocalEvent<ADTFracturedComponent, DidEquipHandEvent>(OnDidEquipHand);
        SubscribeLocalEvent<ADTFracturedComponent, DidUnequipHandEvent>(OnDidUnequipHand);
        SubscribeLocalEvent<GunComponent, GunRefreshModifiersEvent>(OnGunRefresh);
    }

    private void OnBoneInserted(Entity<ADTBoneComponent> ent, ref OrganGotInsertedEvent args)
    {
        if (!HasComp<MobStateComponent>(args.Target))
            return;

        EnsureComp<ADTBonesComponent>(args.Target);

        if (ent.Comp.Fracture != null || ent.Comp.Splinted)
            Refresh(args.Target);
    }

    private void OnBoneRemoved(Entity<ADTBoneComponent> ent, ref OrganGotRemovedEvent args)
    {
        if (LifeStage(args.Target) >= EntityLifeStage.Terminating)
            return;

        if (ent.Comp.Fracture != null || ent.Comp.Splinted)
            Refresh(args.Target);
    }

    private void OnRefreshSpeed(Entity<ADTFracturedComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (IsIgnoring(ent.Comp))
            return;

        args.ModifySpeed(ent.Comp.SpeedModifier);
    }

    private void OnDoAfterDelay(Entity<ADTFracturedComponent> ent, ref GetDoAfterDelayMultiplierEvent args)
    {
        if (IsIgnoring(ent.Comp))
            return;

        args.Multiplier *= ent.Comp.DoAfterModifier;
    }

    private void OnDidEquipHand(Entity<ADTFracturedComponent> ent, ref DidEquipHandEvent args)
    {
        if (HasComp<GunComponent>(args.Equipped))
            _gun.RefreshModifiers(args.Equipped);
    }

    private void OnDidUnequipHand(Entity<ADTFracturedComponent> ent, ref DidUnequipHandEvent args)
    {
        if (HasComp<GunComponent>(args.Unequipped))
            _gun.RefreshModifiers(args.Unequipped);
    }

    private void OnGunRefresh(Entity<GunComponent> ent, ref GunRefreshModifiersEvent args)
    {
        if (!_container.TryGetContainingContainer(ent.Owner, out var container)
            || !TryComp<ADTFracturedComponent>(container.Owner, out var fractured)
            || IsIgnoring(fractured))
            return;

        if (!TryGetHandSide(container.Owner, ent.Owner, out var left))
            return;

        var spread = left ? fractured.SpreadLeft : fractured.SpreadRight;
        if (spread <= 0f)
            return;

        var bonus = Angle.FromDegrees(spread);
        args.MinAngle += bonus;
        args.MaxAngle += bonus;
    }

    public bool IsIgnoring(ADTFracturedComponent comp)
    {
        return comp.IgnoreUntil > Timing.CurTime;
    }

    public bool TryGetHandSide(EntityUid body, EntityUid item, out bool left)
    {
        left = false;

        if (!_hands.IsHolding(body, item, out var handId) || !_hands.TryGetHand(body, handId, out var hand))
            return false;

        if (hand.Value.Location == HandLocation.Middle)
            return false;

        left = hand.Value.Location == HandLocation.Left;
        return true;
    }

    public static bool IsLeg(ProtoId<OrganCategoryPrototype>? category)
    {
        return category == LegLeft || category == LegRight || category == FootLeft || category == FootRight;
    }

    public static bool IsArm(ProtoId<OrganCategoryPrototype>? category)
    {
        return category == ArmLeft || category == ArmRight || category == HandLeft || category == HandRight;
    }

    public static bool IsLeftSide(ProtoId<OrganCategoryPrototype>? category)
    {
        return category == ArmLeft || category == HandLeft || category == LegLeft || category == FootLeft;
    }

    public bool CanBreak(EntityUid organ, ADTBoneComponent bone)
    {
        return bone.CanBreak && !RoboticPart.IsRobotic(organ);
    }

    public bool TryGetBone(EntityUid body, ProtoId<OrganCategoryPrototype> category, [NotNullWhen(true)] out Entity<ADTBoneComponent>? bone)
    {
        foreach (var organ in Body.EnumerateOrgans<ADTBoneComponent>(body))
        {
            if (organ.Comp1.Category != category)
                continue;

            bone = (organ.Owner, organ.Comp2);
            return true;
        }

        bone = null;
        return false;
    }

    public IEnumerable<(Entity<ADTBoneComponent> Bone, ProtoId<OrganCategoryPrototype>? Category)> EnumerateBones(EntityUid body)
    {
        foreach (var organ in Body.EnumerateOrgans<ADTBoneComponent>(body))
        {
            yield return ((organ.Owner, organ.Comp2), organ.Comp1.Category);
        }
    }

    public bool HasFracture(EntityUid body, ProtoId<OrganCategoryPrototype> category)
    {
        return TryGetBone(body, category, out var bone) && bone.Value.Comp.Fracture != null;
    }

    public bool HasAnyFracture(EntityUid body)
    {
        foreach (var (bone, _) in EnumerateBones(body))
        {
            if (bone.Comp.Fracture != null)
                return true;
        }

        return false;
    }

    public ADTFractureTypePrototype? GetFractureType(ADTBoneComponent bone)
    {
        if (bone.Fracture is not { } id || !Proto.TryIndex(id, out var type))
            return null;

        return type;
    }

    public void Mend(EntityUid body, Entity<ADTBoneComponent> bone)
    {
        if (bone.Comp.Fracture == null)
            return;

        SetFracture(bone, null);
        Refresh(body);
    }

    public void MendAll(EntityUid body)
    {
        var changed = false;
        foreach (var (bone, _) in new List<(Entity<ADTBoneComponent>, ProtoId<OrganCategoryPrototype>?)>(EnumerateBones(body)))
        {
            if (bone.Comp.Fracture == null && !bone.Comp.Splinted)
                continue;

            SetFracture(bone, null);
            changed = true;
        }

        if (changed)
            Refresh(body);
    }

    protected void SetFracture(Entity<ADTBoneComponent> bone, ProtoId<ADTFractureTypePrototype>? fracture)
    {
        bone.Comp.Fracture = fracture;

        if (fracture == null)
        {
            bone.Comp.Splinted = false;
            bone.Comp.SplintDistance = 0f;
            bone.Comp.LockedDamage = 0f;
        }

        Dirty(bone);
    }

    protected void SetSplinted(Entity<ADTBoneComponent> bone, bool splinted, float distance)
    {
        bone.Comp.Splinted = splinted;
        bone.Comp.SplintDistance = splinted ? distance : 0f;
        Dirty(bone);
    }

    protected void SetLockedDamage(Entity<ADTBoneComponent> bone, float locked)
    {
        bone.Comp.LockedDamage = locked;
    }

    protected void SetIgnoreUntil(Entity<ADTFracturedComponent> ent, TimeSpan until)
    {
        ent.Comp.IgnoreUntil = until;
        Dirty(ent);
    }

    public void Refresh(EntityUid body)
    {
        if (_net.IsClient)
            return;

        var speed = 1f;
        var doAfter = 1f;
        var spreadLeft = 0f;
        var spreadRight = 0f;
        var dropLeft = 0f;
        var dropRight = 0f;
        var fall = 0f;
        var locked = 0f;
        var hasSplints = false;
        var hasFractures = false;

        foreach (var (bone, category) in EnumerateBones(body))
        {
            if (bone.Comp.Splinted)
                hasSplints = true;

            if (GetFractureType(bone.Comp) is not { } type)
                continue;

            hasFractures = true;

            if (bone.Comp.Splinted)
                continue;

            locked += bone.Comp.LockedDamage;

            if (IsLeg(category))
            {
                speed *= type.SpeedModifier;
                fall = Math.Max(fall, type.FallChance);
                continue;
            }

            if (!IsArm(category))
                continue;

            doAfter += type.DoAfterModifier;

            if (IsLeftSide(category))
            {
                spreadLeft = Math.Max(spreadLeft, type.SpreadDegrees);
                dropLeft = Math.Max(dropLeft, type.DropChance);
            }
            else
            {
                spreadRight = Math.Max(spreadRight, type.SpreadDegrees);
                dropRight = Math.Max(dropRight, type.DropChance);
            }
        }

        if (!hasFractures && !hasSplints)
        {
            if (RemComp<ADTFracturedComponent>(body))
            {
                _movement.RefreshMovementSpeedModifiers(body);
                RefreshHeldGuns(body);
            }

            return;
        }

        var comp = EnsureComp<ADTFracturedComponent>(body);
        comp.SpeedModifier = speed;
        comp.DoAfterModifier = doAfter;
        comp.SpreadLeft = spreadLeft;
        comp.SpreadRight = spreadRight;
        comp.DropLeft = dropLeft;
        comp.DropRight = dropRight;
        comp.FallChance = fall;
        comp.LockedDamage = locked;
        comp.HasSplints = hasSplints;
        comp.HasFractures = hasFractures;
        Dirty(body, comp);

        _movement.RefreshMovementSpeedModifiers(body);
        RefreshHeldGuns(body);
    }

    protected void RefreshHeldGuns(EntityUid body)
    {
        if (!HasComp<HandsComponent>(body))
            return;

        foreach (var held in _hands.EnumerateHeld(body))
        {
            if (HasComp<GunComponent>(held))
                _gun.RefreshModifiers(held);
        }
    }
}
