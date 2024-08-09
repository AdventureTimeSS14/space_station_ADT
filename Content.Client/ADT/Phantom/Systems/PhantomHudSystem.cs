using Content.Shared.Overlays;
using Content.Shared.StatusIcon.Components;
using Content.Shared.ADT.Phantom.Components;
using Content.Shared.StatusIcon;
using Robust.Shared.Prototypes;
using Content.Client.Overlays;

namespace Content.Client.ADT.Phantom;
public sealed class ShowHauntedIconsSystem : EquipmentHudSystem<ShowHauntedIconsComponent>
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PhantomHolderIconComponent, GetStatusIconsEvent>(OnGetStatusIconsEvent);
    }

    private void OnGetStatusIconsEvent(EntityUid uid, PhantomHolderIconComponent haunted, ref GetStatusIconsEvent args)
    {
        if (!IsActive)// || args.InContainer)
        {
            return;
        }

        args.StatusIcons.Add(_prototype.Index(haunted.StatusIcon));
    }

}

