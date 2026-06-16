using Content.Shared.ADT.MindSlave.Components;
using Content.Shared.StatusIcon.Components;
using Robust.Shared.Prototypes;

namespace Content.Client.ADT.MindSlave;

public sealed class MindSlaveSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MindSlaveComponent, GetStatusIconsEvent>(GetSlaveIcon);
        SubscribeLocalEvent<MindSlaveMasterComponent, GetStatusIconsEvent>(GetMasterIcon);
    }

    private void GetSlaveIcon(Entity<MindSlaveComponent> ent, ref GetStatusIconsEvent args)
    {
        if (_prototype.TryIndex(ent.Comp.StatusIcon, out var iconPrototype))
            args.StatusIcons.Add(iconPrototype);
    }

    private void GetMasterIcon(Entity<MindSlaveMasterComponent> ent, ref GetStatusIconsEvent args)
    {
        if (_prototype.TryIndex(ent.Comp.StatusIcon, out var iconPrototype))
            args.StatusIcons.Add(iconPrototype);
    }
}
