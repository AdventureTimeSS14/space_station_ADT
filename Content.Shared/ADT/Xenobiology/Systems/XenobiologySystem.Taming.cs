using Content.Shared.ADT.Xenobiology.Components;
using Content.Shared.Interaction.Events;

namespace Content.Shared.ADT.Xenobiology.Systems;

/// <summary>
/// This handles slime taming, likely to be expanded in the future.
/// </summary>
public sealed partial class XenobiologySystem
{
    private void InitializeTaming()
    {
        SubscribeLocalEvent<SlimeComponent, InteractionSuccessEvent>(OnTame);
    }

    private void OnTame(Entity<SlimeComponent> ent, ref InteractionSuccessEvent args)
    {
        if (_net.IsClient)
            return;

        if (ent.Comp.Tamer.HasValue)
        {
            _popup.PopupEntity(Loc.GetString("slime-interaction-tame-fail"), args.User, args.User);
            return;
        }

        var (slime, comp) = ent;
        var coords = Transform(slime).Coordinates;

        Spawn(ent.Comp.TameEffect, coords);
        comp.Tamer = args.User;

        _popup.PopupEntity(Loc.GetString("slime-interaction-tame"), args.User, args.User);

        Dirty(ent);
    }
}
