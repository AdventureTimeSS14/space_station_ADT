using System.Linq;
using Content.Shared.ADT.Body.Allergies;
using Content.Shared.Body;
using Content.Shared.Botany.Items.Components;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Swab;

namespace Content.Shared.ADT.Botany.Systems;

public sealed partial class SharedADTBotanySwabSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BotanySwabComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<BotanySwabComponent, BotanySwabDoAfterEvent>(OnDoAfter);
    }

    private void OnAfterInteract(EntityUid uid, BotanySwabComponent swab, AfterInteractEvent args)
    {
        if (args.Target == null || !args.CanReach || !HasComp<BodyComponent>(args.Target))
            return;

        if (swab.PlantData != null || swab.PlantProtoId != null)
        {
            _popup.PopupEntity(Loc.GetString("botany-swab-unusable-bio"), args.User, args.User);
            return;
        }

        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, swab.SwabDelay, new BotanySwabDoAfterEvent(), uid, target: args.Target, used: uid)
        {
            Broadcast = true,
            BreakOnMove = true,
            NeedHand = true,
        });
    }

    private void OnDoAfter(EntityUid uid, BotanySwabComponent swab, BotanySwabDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Args.Target == null)
            return;

        if (!HasComp<BodyComponent>(args.Args.Target))
            return;

        if (TryComp<AllergicComponent>(args.Args.Target.Value, out var allergic))
        {
            if (swab.AllergicTriggers == null)
                swab.AllergicTriggers = allergic.Triggers.ToList();
            else
                swab.AllergicTriggers = swab.AllergicTriggers.Concat(allergic.Triggers).Distinct().ToList();
        }

        if (swab.AllergicTriggers == null)
            swab.AllergicTriggers = new();

        Dirty(uid, swab);
        _popup.PopupEntity(Loc.GetString("botany-swab-used-bio"), args.Args.User, args.Args.User);
        args.Handled = true;
    }
}
