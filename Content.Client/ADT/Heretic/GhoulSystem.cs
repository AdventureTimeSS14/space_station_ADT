//

using Content.Shared.ADT.Heretic.Components;
using Content.Shared.Heretic;
using Content.Shared.StatusIcon.Components;
using Robust.Client.Player;
using Robust.Shared.Prototypes;

namespace Content.Client.ADT.Heretic;

public sealed class GhoulSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    public override void Initialize()
    {
        base.Initialize();

        // ADT: GetStatusIconsEvent is directed, subscribe per-component
        SubscribeLocalEvent<HereticComponent, GetStatusIconsEvent>(OnGetHereticIcons);
        SubscribeLocalEvent<HereticMinionComponent, GetStatusIconsEvent>(OnGetMinionIcons);
    }

    private void OnGetHereticIcons(Entity<HereticComponent> ent, ref GetStatusIconsEvent args)
    {
        if (_player.LocalEntity is not { } player)
            return;

        if (TryComp(player, out HereticMinionComponent? minion) && minion.BoundHeretic == ent.Owner)
            args.StatusIcons.Add(_prototype.Index(minion.MasterIcon));
    }

    private void OnGetMinionIcons(Entity<HereticMinionComponent> ent, ref GetStatusIconsEvent args)
    {
        if (_player.LocalEntity is not { } player)
            return;

        if (ent.Comp.BoundHeretic == player)
            args.StatusIcons.Add(_prototype.Index(ent.Comp.GhoulIcon));
    }
}
