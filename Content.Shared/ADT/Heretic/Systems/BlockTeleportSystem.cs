using Content.Shared.ADT.Blob.Components;
using Content.Shared.ADT.Heretic.Common;
using Content.Shared.Popups;

namespace Content.Shared.ADT.Heretic.Systems;

// ADT: from Goob BlockTeleportSystem, uses Blob's component
public sealed class BlockTeleportSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BlockTeleportComponent, TeleportAttemptEvent>(OnAttempt);
    }

    private void OnAttempt(Entity<BlockTeleportComponent> ent, ref TeleportAttemptEvent args)
    {
        args.Cancelled = true;

        if (args.Message == null)
            return;

        var msg = Loc.GetString(args.Message);
        if (args.Predicted)
            _popup.PopupClient(msg, ent, ent);
        else
            _popup.PopupEntity(msg, ent, ent);
    }
}
