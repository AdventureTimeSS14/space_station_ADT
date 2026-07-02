using Content.Shared.ADT.MindSlave.Components;
using Content.Shared.Antag;
using Content.Shared.StatusIcon;
using Content.Shared.StatusIcon.Components;
using Robust.Client.Player;
using Robust.Shared.Prototypes;

namespace Content.Client.ADT.MindSlave;

/// <summary>
/// Handles client-side MindSlave status icon visibility for masters and enslaved characters.
/// </summary>
public sealed class MindSlaveSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MindSlaveComponent, GetStatusIconsEvent>(GetSlaveIcon);
        SubscribeLocalEvent<MindSlaveMasterComponent, GetStatusIconsEvent>(GetMasterIcon);
    }

    private void GetSlaveIcon(Entity<MindSlaveComponent> ent, ref GetStatusIconsEvent args)
    {
        var viewer = _player.LocalEntity;
        if (viewer == null)
            return;

        if (HasComp<ShowAntagIconsComponent>(viewer))
        {
            AddIcon(args, ent.Comp.StatusIcon);
            return;
        }

        if (ent.Comp.Master == viewer)
        {
            AddIcon(args, ent.Comp.StatusIcon);
            return;
        }

        if (TryComp<MindSlaveComponent>(viewer, out var viewerSlave) && viewerSlave.Master == ent.Comp.Master)
        {
            AddIcon(args, ent.Comp.StatusIcon);
        }
    }

    private void GetMasterIcon(Entity<MindSlaveMasterComponent> ent, ref GetStatusIconsEvent args)
    {
        var viewer = _player.LocalEntity;
        if (viewer == null)
            return;

        if (HasComp<ShowAntagIconsComponent>(viewer))
        {
            AddIcon(args, ent.Comp.StatusIcon);
            return;
        }

        if (TryComp<MindSlaveComponent>(viewer, out var viewerSlave) && viewerSlave.Master == ent.Owner)
        {
            AddIcon(args, ent.Comp.StatusIcon);
            return;
        }

        if (viewer == ent.Owner)
        {
            AddIcon(args, ent.Comp.StatusIcon);
        }
    }

    private void AddIcon(GetStatusIconsEvent args, ProtoId<FactionIconPrototype> iconId)
    {
        if (_prototype.TryIndex(iconId, out var iconPrototype))
            args.StatusIcons.Add(iconPrototype);
    }
}
