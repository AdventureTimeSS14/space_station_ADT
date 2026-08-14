using Content.Shared.ADT.Xenobiology.Components;
using Content.Shared.Interaction.Events;

namespace Content.Shared.ADT.Xenobiology.Systems;

public partial class XenobiologySystem
{
    private void SubscribeTaming()
    {
        SubscribeLocalEvent<SlimeComponent, InteractionSuccessEvent>(OnInteractionSuccess);
    }

    private void OnInteractionSuccess(Entity<SlimeComponent> ent, ref InteractionSuccessEvent args)
    {
        if (_net.IsClient) return;

        if (ent.Comp.Tamer.HasValue)
        {
            _popup.PopupEntity(Loc.GetString("slime-interaction-tame-fail"), args.User, args.User);
            return;
        }

        var (slime, comp) = ent;
        var coords = Transform(slime).Coordinates;

        Spawn(ent.Comp.TameEffect, coords);
        comp.Tamer = args.User;
        comp.Friendship = MathF.Max(comp.Friendship, comp.MinFriendshipToCommand);

        _popup.PopupEntity(Loc.GetString("slime-interaction-tame"), args.User, args.User);

        Dirty(ent);
    }
}