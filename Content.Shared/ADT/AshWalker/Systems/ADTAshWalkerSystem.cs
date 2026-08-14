using Content.Shared.ADT.AshWalker.Components;
using Content.Shared.Popups;
using Content.Shared.Weapons.Ranged.Events;

namespace Content.Shared.ADT.AshWalker.Systems;

public sealed class ADTAshWalkerSystem : EntitySystem
{
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
        _popup.PopupClient(Loc.GetString("adt-ash-walker-no-guns"), ent, ent);
    }
}
