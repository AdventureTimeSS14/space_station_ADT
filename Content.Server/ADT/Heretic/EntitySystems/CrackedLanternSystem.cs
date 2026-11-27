//

using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos.Components;
using Content.Shared.Flash;
using Content.Shared.Heretic.Components;
using Content.Shared.Interaction.Events;
using Robust.Shared.Timing;

namespace Content.Server.Heretic.EntitySystems;

public sealed class CrackedLanternSystem : EntitySystem
{
    [Dependency] private readonly SharedFlashSystem _flash = default!;
    [Dependency] private readonly FlammableSystem _flammable = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CrackedLanternComponent, UseInHandEvent>(OnUse);
    }

    private void OnUse(Entity<CrackedLanternComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        _flash.FlashArea(ent, args.User, ent.Comp.Range, TimeSpan.FromSeconds(ent.Comp.Duration), displayPopup: true);

        foreach (var uid in _lookup.GetEntitiesInRange(ent, ent.Comp.Range))
        {
            if (uid == args.User)
                continue;

            if (!TryComp(uid, out FlammableComponent? flammable))
                continue;

            _flammable.AdjustFireStacks(uid, ent.Comp.FireStacks, flammable, ignite: true);
        }
    }
}
