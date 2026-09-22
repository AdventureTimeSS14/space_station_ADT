using Content.Shared.ADT.AshWalker.Components;
using Content.Shared.Popups;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Timing;

namespace Content.Shared.ADT.AshWalker.Systems;

public sealed class ADTAshWalkerSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTAshWalkerComponent, ShotAttemptedEvent>(OnShotAttempted);
    }

    private void OnShotAttempted(Entity<ADTAshWalkerComponent> ent, ref ShotAttemptedEvent args)
    {
        if (!ent.Comp.BlockGuns)
            return;

        if (args.Cancelled)
            return;

        args.Cancel();

        if (!_timing.IsFirstTimePredicted)
            return;

        if (_timing.CurTime < ent.Comp.NextGunPopup)
            return;

        ent.Comp.NextGunPopup = _timing.CurTime + ent.Comp.GunPopupCooldown;
        _popup.PopupClient(Loc.GetString("adt-ash-walker-no-guns"), ent.Owner, ent.Owner);
    }
}
