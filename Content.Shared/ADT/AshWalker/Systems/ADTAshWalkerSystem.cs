using Content.Shared.ADT.AshWalker.Components;
using Content.Shared.ADT.ModSuits;
using Content.Shared.Inventory.Events;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.ADT.AshWalker.Systems;

public sealed class ADTAshWalkerSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly TagSystem _tag = default!;

    private static readonly ProtoId<TagPrototype> HardsuitTag = "Hardsuit";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTAshWalkerComponent, ShotAttemptedEvent>(OnShotAttempted);
        SubscribeLocalEvent<ADTAshWalkerComponent, IsEquippingTargetAttemptEvent>(OnEquipAttempt);
    }

    private void OnShotAttempted(Entity<ADTAshWalkerComponent> ent, ref ShotAttemptedEvent args)
    {
        if (!ent.Comp.BlockGuns)
            return;

        if (args.Cancelled)
            return;

        if (_whitelist.IsWhitelistPass(ent.Comp.Whitelist, args.Used))
            return;

        args.Cancel();

        if (!_timing.IsFirstTimePredicted)
            return;

        if (_timing.CurTime < ent.Comp.NextGunPopup)
            return;

        ent.Comp.NextGunPopup = _timing.CurTime + ent.Comp.GunPopupCooldown;
        _popup.PopupClient(Loc.GetString("adt-ash-walker-no-guns"), ent.Owner, ent.Owner);
    }

    private void OnEquipAttempt(Entity<ADTAshWalkerComponent> ent, ref IsEquippingTargetAttemptEvent args)
    {
        if (!_tag.HasTag(args.Equipment, HardsuitTag) && !HasComp<ModSuitComponent>(args.Equipment))
            return;

        args.Reason = "adt-ashwalker-no-spacesuit";
        args.Cancel();
    }
}
