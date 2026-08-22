using Content.Server.ADT.Salvage.Components;
using Content.Shared.Damage.Systems;
using Content.Server.Medical;
using Content.Shared.Body.Components;
using Content.Shared.Damage.Components;
using Content.Shared.Nutrition.Components;
using Content.Shared.Interaction.Events;
using Content.Server.Interaction;
using Content.Shared.ADT.Salvage.Components;
using Content.Shared.Chasm;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Content.Shared.Medical;
using Content.Shared.Body;

namespace Content.Server.ADT.Salvage.Systems;

public sealed class JaunterSystem : EntitySystem
{
    private const int MaxJumpAttempts = 32;

    [Dependency] private readonly JaunterPortalSystem _portal = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly InteractionSystem _interaction = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedStaminaSystem _stamina = default!;
    [Dependency] private readonly VomitSystem _vomit = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<JaunterComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<JaunterComponent, BeforeChasmFallingEvent>(OnBeforeFall);
        SubscribeLocalEvent<ContainerManagerComponent, BeforeChasmFallingEvent>(Relay);
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
        if (args.Cancelled)
            return;

        var target = args.Entity;

        args.Cancelled = true;

        var immunity = EnsureComp<ADTChasmImmunityComponent>(target);
        immunity.Until = _timing.CurTime + comp.ChasmImmunity;
        Dirty(target, immunity);

        if (!TryGetJumpCoords(target, comp, out var newCoords))
            return;

        _transform.SetCoordinates(target, newCoords);
        _transform.AttachToGridOrMap(target, Transform(target));
        _audio.PlayPvs(new SoundPathSpecifier("/Audio/Items/Mining/fultext_launch.ogg"), target);

        if (TryComp<StaminaComponent>(target, out var stam))
        {
            var need = MathF.Max(0.01f, stam.CritThreshold - stam.StaminaDamage);
            _stamina.TakeStaminaDamage(target, need, stam);
        }

        if (HasComp<OrganComponent>(target) && HasComp<HungerComponent>(target))
        {
            _vomit.Vomit(target);
        }

        if (target != uid && comp.DeleteOnUse)
            QueueDel(uid);
    }

    private bool TryGetJumpCoords(EntityUid target, JaunterComponent comp, out EntityCoordinates coords)
    {
        coords = default;

        if (_portal.GetRandomBeacon() is { } beacon)
        {
            comp.BeaconMode = true;
            coords = Transform(beacon).Coordinates;
            return true;
        }

        var xform = Transform(target);
        var origin = xform.Coordinates;

        for (var i = 0; i < MaxJumpAttempts; i++)
        {
            var candidate = new EntityCoordinates(xform.ParentUid,
                origin.X + _random.NextFloat(-5f, 5f),
                origin.Y + _random.NextFloat(-5f, 5f));

            if (!_interaction.InRangeUnobstructed(target, candidate, -1f))
                continue;

            if (_lookup.GetEntitiesInRange<ChasmComponent>(candidate, 1f).Count > 0)
                continue;

            coords = candidate;
            return true;
        }

        return false;
    }

    private void Relay(EntityUid uid, ContainerManagerComponent comp, ref BeforeChasmFallingEvent args)
    {
        if (args.Cancelled)
            return;

        var contained = new List<EntityUid>();
        foreach (var container in comp.Containers.Values)
        {
            contained.AddRange(container.ContainedEntities);
        }

        foreach (var entity in contained)
        {
            if (args.Cancelled)
                return;

            if (TerminatingOrDeleted(entity))
                continue;

            RaiseLocalEvent(entity, ref args);
        }
    }
}
