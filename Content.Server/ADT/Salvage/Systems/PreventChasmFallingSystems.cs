using Content.Server.ADT.Salvage.Components;
using Content.Server.Interaction;
using Content.Shared.ADT.Salvage.Components;
using Content.Shared.Chasm;
using Content.Shared.Inventory;
using Content.Shared.Damage.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Server.ADT.Salvage.Systems;

public sealed class PreventChasmFallingSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly InteractionSystem _interaction = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly JaunterSystem _jaunter = default!;


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PreventChasmFallingComponent, BeforeChasmFallingEvent>(OnBeforeFall);
        SubscribeLocalEvent<InventoryComponent, BeforeChasmFallingEvent>(Relay);
    }

    private void OnBeforeFall(EntityUid uid, PreventChasmFallingComponent comp, ref BeforeChasmFallingEvent args)
    {
        // Cancel the fall and teleport the victim directly to a random station beacon with effects.
        args.Cancelled = true;
        _jaunter.TeleportToRandomBeaconWithFx(args.Entity);

        // Apply post-teleport effects matching portal entry
        if (TryComp<StaminaComponent>(args.Entity, out var stam))
        {
            var need = MathF.Max(0.01f, stam.CritThreshold - stam.StaminaDamage);
            var stamina = EntitySystem.Get<Content.Shared.Damage.Systems.SharedStaminaSystem>();
            stamina.TakeStaminaDamage(args.Entity, need, stam);
        }
        var vomit = EntitySystem.Get<Content.Server.Medical.VomitSystem>();
        vomit.Vomit(args.Entity);

        _audio.PlayPvs(new SoundPathSpecifier("/Audio/Items/Mining/fultext_launch.ogg"), args.Entity);

        // Consume the jaunter if it is not the entity falling (e.g., in inventory).
        if (args.Entity != uid)
            QueueDel(uid);
    }

    private void Relay(EntityUid uid, InventoryComponent comp, ref BeforeChasmFallingEvent args)
    {
        if (!TryComp<ContainerManagerComponent>(uid, out var containerManager))
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
