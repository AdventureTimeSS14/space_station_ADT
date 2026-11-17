using Content.Shared.ADT.Chaplain.Components;
using Content.Shared.ADT.Chaplain.Systems;
using Content.Shared.StatusIcon.Components;
using Robust.Shared.Prototypes;

namespace Content.Client.ADT.Chaplain;

public sealed class ChaplainSystem : SharedChaplainSystem
{
    [Dependency] private IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChaplainComponent, GetStatusIconsEvent>(OnGetStatusIcons);
    }

    private void OnGetStatusIcons(Entity<ChaplainComponent> ent, ref GetStatusIconsEvent args)
    {
        if (_proto.TryIndex(ent.Comp.StatusIcon, out var icon))
            args.StatusIcons.Add(icon);
    }
}
