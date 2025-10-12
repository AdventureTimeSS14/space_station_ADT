using Content.Shared.Revolutionary.Components;
using Content.Shared.Revolutionary;
using Content.Shared.StatusIcon.Components;
using Robust.Shared.Prototypes;
using Content.Shared.ADT.BloodBrothers;
using Robust.Client.Player;

namespace Content.Client.ADT.BloodBrothers;

/// <summary>
/// Used for the client to get status icons from other brothers.
/// </summary>
public sealed class BloodBrotherSystem : SharedBloodBrothersSystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BloodBrotherComponent, GetStatusIconsEvent>(GetBlotherIcon);
        SubscribeLocalEvent<BloodBrotherLeaderComponent, GetStatusIconsEvent>(GetBrotherLeadIcon);
    }

    private void GetBlotherIcon(Entity<BloodBrotherComponent> ent, ref GetStatusIconsEvent args)
    {
        var player = _player.LocalEntity;

        if (!TryComp<BloodBrotherComponent>(player, out var viewerBrother))
            return;
        if (viewerBrother.Leader != ent.Comp.Leader)
            return;

        if (_prototype.TryIndex(ent.Comp.StatusIcon, out var iconPrototype))
            args.StatusIcons.Add(iconPrototype);
    }
    private void GetBrotherLeadIcon(Entity<BloodBrotherLeaderComponent> ent, ref GetStatusIconsEvent args)
    {
        var player = _player.LocalEntity;

        if (!TryComp<BloodBrotherComponent>(player, out var viewerBrother))
            return;

        if (viewerBrother.Leader != ent.Owner)
            return;

        if (_prototype.TryIndex(ent.Comp.StatusIcon, out var iconPrototype))
            args.StatusIcons.Add(iconPrototype);
    }
}