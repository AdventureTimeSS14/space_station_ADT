using Content.Server.ADT.Salvage.Components;
using Content.Shared.Interaction.Events;
using Content.Server.Interaction;
using Content.Shared.ADT.Salvage.Components;
using Content.Shared.Chasm;
using Content.Shared.Inventory;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Server.ADT.Salvage.Systems;

public sealed class JaunterSystem : EntitySystem
{
    [Dependency] private readonly JaunterPortalSystem _portal = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly InteractionSystem _interaction = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<JaunterComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<JaunterComponent, BeforeChasmFallingEvent>(OnBeforeFall);
        SubscribeLocalEvent<InventoryComponent, BeforeChasmFallingEvent>(Relay);
    }

    private void OnUseInHand(EntityUid uid, JaunterComponent comp, UseInHandEvent args)
    {
        if (args.Handled)
            return;

        _portal.SpawnLinkedPortal(args.User);
        QueueDel(uid);
        args.Handled = true;
    }

    private void OnBeforeFall(EntityUid uid, JaunterComponent comp, ref BeforeChasmFallingEvent args)
    {
        args.Cancelled = true;
        var coordsValid = false;
        var coords = Transform(args.Entity).Coordinates;
        EntityCoordinates newCoords;

        while (!coordsValid)
        {
            var randombeacon = _portal.GetRandomBeacon();
            if (randombeacon != null)
            {
                newCoords = Transform(randombeacon.Value).Coordinates;
                comp.BeaconMode = true;
            }
            else
            {
                newCoords = new EntityCoordinates(Transform(args.Entity).ParentUid, coords.X +
                    _random.NextFloat(-5f, 5f), coords.Y +
                    _random.NextFloat(-5f, 5f));
            }

            if (_interaction.InRangeUnobstructed(args.Entity, newCoords, -1f) && _lookup.GetEntitiesInRange<ChasmComponent>(newCoords, 1f).Count <= 0 || comp.BeaconMode == true)
            {
                _transform.SetCoordinates(args.Entity, newCoords);
                _transform.AttachToGridOrMap(args.Entity, Transform(args.Entity));
                _audio.PlayPvs(new SoundPathSpecifier("/Audio/Items/Mining/fultext_launch.ogg"), args.Entity);

                if (args.Entity != uid && comp.DeleteOnUse)
                    QueueDel(uid);

                coordsValid = true;
            }
        }
    }

    private void Relay(EntityUid uid, InventoryComponent comp, ref BeforeChasmFallingEvent args)
    {
        if (!HasComp<ContainerManagerComponent>(uid))
            return;

        RelayEvent(uid, ref args);
    }

    private void RelayEvent(EntityUid uid, ref BeforeChasmFallingEvent ev)
    {
        if (!TryComp<ContainerManagerComponent>(uid, out var containerManager))
            return;

        foreach (var container in containerManager.Containers.Values)
        {
            if (ev.Cancelled)
                break;

            foreach (var entity in container.ContainedEntities)
            {
                RaiseLocalEvent(entity, ref ev);
                if (ev.Cancelled)
                    break;

                RelayEvent(entity, ref ev);
            }
        }
    }
}
