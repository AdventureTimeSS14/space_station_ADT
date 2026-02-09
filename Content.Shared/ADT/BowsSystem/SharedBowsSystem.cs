using Content.Shared.ADT.BowsSystem.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Interaction.Events;
using Content.Shared.Projectiles;
using Content.Shared.Popups;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Wieldable.Components;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Shared.ADT.BowsSystem;

public sealed partial class BowsSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ExpendedBowsComponent, GunShotEvent>(OnShoot);
        SubscribeLocalEvent<ExpendedBowsComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<ExpendedBowsComponent, GunRefreshModifiersEvent>(EditSpeed);
    }
    
    public void OnShoot(Entity<ExpendedBowsComponent> bow, ref GunShotEvent args)
    {
        bow.Comp.StepOfTension=bow.Comp.MinTension;
    }

    public void OnUseInHand(Entity<ExpendedBowsComponent> bow, ref UseInHandEvent args)
    {
        bow.Comp.coldownStart = _timing.CurTime + TimeSpan.FromSeconds(bow.Comp.floatToColdown);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ExpendedBowsComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            if (_timing.CurTime < comp.coldownStart)
                continue;
            if(!TryComp<WieldableComponent>(uid,out var wieldedcomp))
                continue;
            if (wieldedcomp.Wielded == false)
            {
                comp.StepOfTension=comp.MinTension;
                continue;
            }
            if (wieldedcomp.User is not {} owner)
                return;
            if (comp.StepOfTension >= comp.MaxTension)
                continue;

            comp.StepOfTension++;
            _popup.PopupClient(Loc.GetString(comp.TensionAndLoc[comp.StepOfTension],("user", owner)), uid, owner);
            _audio.PlayPvs(comp.bowSound, owner);
            comp.coldownStart = _timing.CurTime + TimeSpan.FromSeconds(comp.floatToColdown);
        }
    }

    public void EditSpeed(Entity<ExpendedBowsComponent> bow, ref GunRefreshModifiersEvent args)
    {
        args.ProjectileSpeed =args.ProjectileSpeed * bow.Comp.TensionAndModieferSpeed[bow.Comp.StepOfTension];
    }
}