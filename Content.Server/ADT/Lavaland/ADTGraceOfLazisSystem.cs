using Content.Shared.ADT.Lavaland;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Kitchen.Components;
using Content.Shared.Popups;
using Content.Shared.Whitelist;

namespace Content.Server.ADT.Lavaland;

public sealed class ADTGraceOfLazisSystem : EntitySystem
{
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTGraceOfLazisComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ADTGraceOfLazisComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<ADTGraceOfLazisComponent, ADTGraceCutDoAfterEvent>(OnCut);
        SubscribeLocalEvent<ADTGraceOfLazisComponent, ExaminedEvent>(OnExamined);
    }

    private void OnMapInit(Entity<ADTGraceOfLazisComponent> ent, ref MapInitEvent args)
    {
        UpdateVisuals(ent);
    }

    private void OnInteractUsing(Entity<ADTGraceOfLazisComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled || !CanCut(ent, args.Used))
            return;

        args.Handled = true;

        var doAfter = new DoAfterArgs(EntityManager, args.User, ent.Comp.Delay, new ADTGraceCutDoAfterEvent(), ent.Owner, ent.Owner, args.Used)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            NeedHand = true,
            BlockDuplicate = true,
            CancelDuplicate = true,
        };

        if (_doAfter.TryStartDoAfter(doAfter))
            _popup.PopupEntity(Loc.GetString("adt-grace-cut-start"), ent.Owner, args.User);
    }

    private bool CanCut(Entity<ADTGraceOfLazisComponent> ent, EntityUid used)
    {
        if (ent.Comp.Whitelist != null)
            return _whitelist.IsWhitelistPass(ent.Comp.Whitelist, used);

        return HasComp<SharpComponent>(used);
    }

    private void OnCut(Entity<ADTGraceOfLazisComponent> ent, ref ADTGraceCutDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;

        var portion = Spawn(ent.Comp.Portion, Transform(ent.Owner).Coordinates);

        _hands.PickupOrDrop(args.User, portion);
        _popup.PopupEntity(Loc.GetString("adt-grace-cut-success"), ent.Owner, args.User);

        ent.Comp.Portions--;

        if (ent.Comp.Portions > 0)
        {
            UpdateVisuals(ent);
            return;
        }

        _popup.PopupEntity(Loc.GetString("adt-grace-empty"), ent.Owner, args.User, PopupType.MediumCaution);

        Spawn(ent.Comp.Leftover, Transform(ent.Owner).Coordinates);
        QueueDel(ent.Owner);
    }

    private void OnExamined(Entity<ADTGraceOfLazisComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("adt-grace-examine", ("portions", ent.Comp.Portions)));
    }

    private void UpdateVisuals(Entity<ADTGraceOfLazisComponent> ent)
    {
        var stage = ent.Comp.Portions switch
        {
            > 30 => 4,
            > 20 => 3,
            > 10 => 2,
            _ => 1,
        };

        _appearance.SetData(ent.Owner, ADTGraceOfLazisVisuals.Stage, stage);
    }
}
