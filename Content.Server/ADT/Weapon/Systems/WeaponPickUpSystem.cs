using Content.Server.Hands.Systems;
using Content.Server.Popups;
using Content.Shared.ADT.Weapon.Components;
using Robust.Shared.Containers;

namespace Content.Server.ADT.Weapon.Systems;

/// <summary>
/// Teleports the nearest pickable weapon on the map into the user's hands when the pull action is used.
/// </summary>
public sealed class WeaponPickUpSystem : EntitySystem
{
    [Dependency] private readonly HandsSystem _hands = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly PopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PullingGlovesComponent, WeaponPullActionEvent>(OnActionPressed);
    }

    private void OnActionPressed(Entity<PullingGlovesComponent> ent, ref WeaponPullActionEvent args)
    {
        if (args.Handled)
            return;

        var user = args.Performer;
        var userCoords = Transform(user).Coordinates;
        var userMapPos = _transform.GetMapCoordinates(user);
        var userMapId = userMapPos.MapId;

        EntityUid? nearestWeapon = null;
        var nearestDistanceSq = float.MaxValue;

        var query = EntityQueryEnumerator<PickableWeaponComponent, TransformComponent>();
        while (query.MoveNext(out var weapon, out _, out var weaponXform))
        {
            if (weapon == user || weaponXform.MapID != userMapId || _container.IsEntityInContainer(weapon))
                continue;

            var weaponMapPos = _transform.GetMapCoordinates(weapon, weaponXform);
            var distanceSq = (weaponMapPos.Position - userMapPos.Position).LengthSquared();

            if (distanceSq >= nearestDistanceSq)
                continue;

            nearestDistanceSq = distanceSq;
            nearestWeapon = weapon;
        }

        if (nearestWeapon == null)
        {
            _popup.PopupEntity(Loc.GetString("weapon-pickup-failed"), user, user);
            args.Handled = true;
            return;
        }

        var maxDistance = ent.Comp.MaxDistance;
        if (nearestDistanceSq > maxDistance * maxDistance)
        {
            _popup.PopupEntity(Loc.GetString("weapon-pickup-too-far"), user, user);
            args.Handled = true;
            return;
        }

        var weaponCoords = Transform(nearestWeapon.Value).Coordinates;
        Transform(nearestWeapon.Value).Coordinates = userCoords;

        if (!_hands.TryPickupAnyHand(user, nearestWeapon.Value))
        {
            Transform(nearestWeapon.Value).Coordinates = weaponCoords;
            _popup.PopupEntity(Loc.GetString("weapon-pickup-no-hands"), user, user);
            args.Handled = true;
            return;
        }

        args.Handled = true;
    }
}
