using System.Numerics;
using Content.Shared.ADT.Heretic.Components;
using Content.Shared.ADT.Heretic.Systems;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Throwing;
using Robust.Shared.Physics;

namespace Content.Server.ADT.Heretic.EntitySystems.PathSpecific;

public sealed partial class StarGazerGrabSystem : EntitySystem
{
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedHereticSystem _heretic = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly ThrowingSystem _throw = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private const float ThrowSpeed = 12f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InteractHandEvent>(OnInteractHand);
        SubscribeLocalEvent<StarGazerComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnInteractHand(InteractHandEvent args)
    {
        if (args.Handled)
            return;

        var gazer = args.User;
        var target = args.Target;

        if (!HasComp<StarGazerComponent>(gazer))
            return;

        if (target == gazer)
            return;

        if (!HasComp<MobStateComponent>(target) || _mobState.IsDead(target))
            return;

        if (_heretic.IsHereticOrGhoul(target) || HasComp<StarGazerComponent>(target))
            return;

        if (!TryComp<HandsComponent>(gazer, out var hands) || hands.Count <= 0)
            return;

        args.Handled = true;
        _hands.TryForcePickupAnyHand(gazer, target, checkActionBlocker: false);
    }

    private void OnMobStateChanged(Entity<StarGazerComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Alive)
            return;

        if (GetHeld(ent.Owner) is not { } held)
            return;

        var dir = _transform.GetWorldRotation(ent).ToWorldVec() * -1f;
        if (dir.LengthSquared() < 0.01f)
            dir = Vector2.UnitX;

        _hands.TryDrop(ent.Owner, held, checkActionBlocker: false);
        _throw.TryThrow(held, dir.Normalized(), ThrowSpeed, ent.Owner, recoil: false, animated: false, playSound: true);
    }

    private EntityUid? GetHeld(EntityUid gazer)
    {
        if (!TryComp<HandsComponent>(gazer, out var hands))
            return null;

        foreach (var handId in hands.SortedHands)
        {
            if (!_hands.TryGetHeldItem((gazer, hands), handId, out var item))
                continue;

            if (!HasComp<MobStateComponent>(item.Value))
                continue;

            return item.Value;
        }

        return null;
    }
}