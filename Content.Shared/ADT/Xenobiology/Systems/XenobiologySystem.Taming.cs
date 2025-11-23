using Content.Shared.ADT.Xenobiology.Components;
using Content.Shared.Interaction.Events;

namespace Content.Shared.ADT.Xenobiology.Systems;

/// <summary>
/// This handles slime taming, likely to be expanded in the future.
/// </summary>
public sealed partial class XenobiologySystem
{
    private void InitializeTaming() =>
        SubscribeLocalEvent<SlimeComponent, InteractionSuccessEvent>(OnTame);

    private void OnTame(Entity<SlimeComponent> ent, ref InteractionSuccessEvent args)
    {
        if (ent.Comp.Tamer.HasValue
            || _net.IsClient)
            return;

        var (slime, comp) = ent;
        var coords = Transform(slime).Coordinates;
        var user = args.User;

        // Hearts VFX - Slime taming is seperate to core Pettable Component/System
        Spawn(ent.Comp.TameEffect, coords);
        comp.Tamer = user;
        Dirty(ent);
    }
}
