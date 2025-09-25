using Content.Shared.ADT.Systems.PickupHumans;
using Content.Shared.ADT.Components.PickupHumans;
using Content.Shared.Construction.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Physics;
using Content.Shared.Projectiles;
using Content.Shared.Stacks;
using Content.Shared.Throwing;
using Robust.Server.Audio;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Collections;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
using Robust.Shared.Random;
using Content.Server.Atmos.Components;
using Content.Server.Stack;
using System.Linq;
using System.Numerics;

using Robust.Server.GameObjects;

namespace Content.Server.ADT.BlueSpaceCrystalTeleport;

public sealed class BsCrystalTeleportSystem : EntitySystem
{
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly StackSystem _stacks = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly SharedPickupHumansSystem _pickupsys = default!;

    private float CountToRadius;


    public override void Initialize()
    {
        SubscribeLocalEvent<BsCrystalTeleportComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<BsCrystalTeleportComponent, ProjectileHitEvent>(OnProjectileHit);
    }

    private void OnUseInHand(EntityUid uid, BsCrystalTeleportComponent component, UseInHandEvent args)
    {
        if (args.Handled)
            return;

        if (TryTeleport(args.User, component.TeleportRadius, component.TeleportSound))
        {
            if (TryComp<StackComponent>(uid, out var stackComp))
                _stacks.Use(uid, 1, stackComp);
            args.Handled = true;
        }
    }
    private void OnProjectileHit(Entity<BsCrystalTeleportComponent> uid, ref ProjectileHitEvent args)
    {
        TryTeleport(args.Target, uid.Comp.TeleportRadiusThrow, uid.Comp.TeleportSound);
    }



    private bool TryTeleport(EntityUid entity, float radius, SoundSpecifier sound)
    {
        var targetCoords = SelectRandomTileInRange(entity, radius);
        if (targetCoords == null)
            return false;

        _audio.PlayPvs(sound, entity);
        return true;
    }
    private EntityCoordinates? SelectRandomTileInRange(EntityUid uid, float radius)
    {
        if (HasComp<TakenHumansComponent>(uid)
        && TryComp<PickupHumansComponent>(uid, out var pickupComp))
        {
             _pickupsys.DropFromHands(pickupComp.User, pickupComp.Target);
        }
        EntityCoordinates coords = Transform(uid).Coordinates;
        var newCoords = new EntityCoordinates(Transform(uid).ParentUid, coords.X + _random.NextFloat(-radius, +radius), coords.Y + _random.NextFloat(-radius, +radius));

        _transform.SetCoordinates(uid, newCoords);
        return newCoords;
    }
}
