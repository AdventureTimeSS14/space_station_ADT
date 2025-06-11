using Content.Shared.ADT.Phantom;
using Content.Shared.ADT.Phantom.Components;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;
using Content.Shared.StatusIcon.Components;
using Robust.Client.UserInterface;
using Content.Client.UserInterface.Systems.Phantom;
using Content.Client.Popups;
using Content.Shared.Actions;
using Content.Shared.Tag;
using Content.Shared.Whitelist;
using System.Linq;
using Robust.Shared.Timing;

namespace Content.Client.ADT.Phantom;

public sealed partial class PhantomSystem
{
    [Dependency] private readonly IUserInterfaceManager _ui = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _time = default!;

    private void InitializeUI()
    {
        base.Initialize();

        SubscribeLocalEvent<PhantomComponent, OpenPhantomStylesMenuActionEvent>(OnRequestStyleMenu);
        SubscribeLocalEvent<PhantomComponent, FreedomFinaleActionEvent>(OnRequestFreedomMenu);

        SubscribeNetworkEvent<RequestPhantomVesselMenuEvent>(OnRequestVesselMenu);
        SubscribeNetworkEvent<OpenRadioFakerMenuEvent>(OnFakerOpen);
    }

    private void OnRequestStyleMenu(EntityUid uid, PhantomComponent component, OpenPhantomStylesMenuActionEvent args)
    {
        if (!_time.IsFirstTimePredicted)
            return;

        if (args.Handled)
            return;

        args.Handled = true;

        var controller = _ui.GetUIController<PhantomRadialUIController>();

        controller.OpenMenu();
        controller.PopulateStyles();
    }

    private void OnRequestFreedomMenu(EntityUid uid, PhantomComponent component, FreedomFinaleActionEvent args)
    {
        if (!_time.IsFirstTimePredicted)
            return;

        if (args.Handled)
            return;

        if (component.FinalAbilityUsed)
        {
            var selfMessage = Loc.GetString("phantom-final-already");
            _popup.PopupPredicted(selfMessage, null, uid, uid);
            return;
        }

        List<EntProtoId> prototypes = new();

        foreach (var prototype in _proto.EnumeratePrototypes<EntityPrototype>())
        {
            if (!prototype.TryGetComponent<InstantActionComponent>(out var actionComp))
                continue;

            if (!prototype.TryGetComponent<TagComponent>(out var tag))
                continue;

            if (actionComp.Icon == null)
                continue;

            if (!tag.Tags.ToList().Contains("PhantomFreedom"))
                continue;

            prototypes.Add(prototype.ID);
        }

        prototypes.Sort();

        var controller = _ui.GetUIController<PhantomRadialUIController>();

        controller.OpenMenu();
        controller.PopulateFreedom(prototypes);

        args.Handled = true;
    }

    private void OnRequestVesselMenu(RequestPhantomVesselMenuEvent ev)
    {
        var controller = _ui.GetUIController<PhantomRadialUIController>();

        controller.OpenMenu();
        controller.PopulateVessels(ev.Vessels);
    }

    private void OnFakerOpen(OpenRadioFakerMenuEvent ev)
    {
        _ui.GetUIController<RadioFakerUIController>()
            .OpenMenu(ev.User, ev.Target, ev.Channels);
    }
}
