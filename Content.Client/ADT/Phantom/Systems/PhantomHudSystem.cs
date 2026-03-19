using Content.Shared.Overlays;
using Content.Shared.StatusIcon.Components;
using Content.Shared.ADT.Phantom.Components;
using Content.Shared.StatusIcon;
using Robust.Shared.Prototypes;
using Content.Client.Overlays;
using Content.Shared.Antag;
using Robust.Client.Player;
using Content.Shared.Bible.Components;

namespace Content.Client.ADT.Phantom;
public sealed class ShowHauntedIconsSystem : EquipmentHudSystem<ShowHauntedIconsComponent>
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PhantomHolderComponent, GetStatusIconsEvent>(OnGetStatusIconsEvent);
    }

    private void OnGetStatusIconsEvent(EntityUid uid, PhantomHolderComponent haunted, ref GetStatusIconsEvent args)
    {
        var ent = _player.LocalEntity;
        if (ent == null)
            return;

        if (HasComp<ShowAntagIconsComponent>(ent) ||
            HasComp<PhantomComponent>(ent) ||
            HasComp<PhantomPuppetComponent>(ent) ||
            HasComp<ChaplainComponent>(ent))
        {
            args.StatusIcons.Add(_prototype.Index(haunted.StatusIcon));
            return;
        }

        if (!IsActive)
            return;

        args.StatusIcons.Add(_prototype.Index(haunted.StatusIcon));
    }

}

