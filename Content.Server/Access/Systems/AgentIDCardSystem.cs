using Content.Server.Clothing.Systems;
using Content.Server.Implants;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.ADT.NanoChat; // ADT-Tweak
using Content.Shared.Clothing.Components;
using Content.Shared.Implants;
using Content.Shared.Inventory;
using Content.Shared.PDA;
using Content.Shared.UserInterface; // ADT-Tweak
using Robust.Server.GameObjects; // ADT-Tweak

namespace Content.Server.Access.Systems;

/// <inheritdoc />
public sealed partial class AgentIdCardSystem : SharedAgentIdCardSystem
{
    [Dependency] private SharedIdCardSystem _card = default!;
    [Dependency] private ChameleonClothingSystem _chameleon = default!;
    [Dependency] private ChameleonControllerSystem _chamController = default!;
    // ADT-Tweak-Start
    [Dependency] private UserInterfaceSystem _uiSystem = default!;
    [Dependency] private SharedNanoChatSystem _nanoChat = default!;

    [SubscribeLocalEvent]
    private void OnChameleonControllerOutfitChangedItem(Entity<AgentIDCardComponent> ent, ref InventoryRelayedEvent<ChameleonControllerOutfitSelectedEvent> args)
    {
        if (!TryComp<IdCardComponent>(ent, out var idCardComp))
            return;

        ProtoMan.Resolve(args.Args.ChameleonOutfit.Job, out var jobProto);

        var jobIcon = args.Args.ChameleonOutfit.Icon ?? jobProto?.Icon;
        var jobName = args.Args.ChameleonOutfit.Name ?? jobProto?.Name ?? "";

        if (jobIcon != null)
            _card.TryChangeJobIcon(ent, ProtoMan.Index(jobIcon.Value), idCardComp);

        if (jobName != "")
            _card.TryChangeJobTitle(ent, Loc.GetString(jobName), idCardComp);

        // If you have forced departments use those over the jobs actual departments.
        if (args.Args.ChameleonOutfit.Departments?.Count > 0)
            _card.TryChangeJobDepartment(ent, args.Args.ChameleonOutfit.Departments, idCardComp);
        else if (jobProto != null)
            _card.TryChangeJobDepartment(ent, jobProto, idCardComp);

        // Ensure that you chameleon IDs in PDAs correctly. Yes this is sus...

        // There is one weird interaction: If the job / icon don't match the PDAs job the chameleon will be updated
        // to the PDAs IDs sprite but the icon and job title will not match. There isn't a way to get around this
        // really as there is no tie between job -> pda or pda -> job.

        var idSlotGear = _chamController.GetGearForSlot(args, "id");
        if (idSlotGear == null)
            return;

        var proto = ProtoMan.Index(idSlotGear);
        if (!proto.TryComp<PdaComponent>(out var comp, EntityManager.ComponentFactory))
            return;

        if (TryComp<ChameleonClothingComponent>(ent, out var chameleonComp) && chameleonComp.CanBeSetByController)
            _chameleon.SetSelectedPrototype(ent, comp.IdCard, component: chameleonComp);
    }

    // ADT-Tweak-Start
    [SubscribeLocalEvent]
    private void OnNumberChanged(Entity<AgentIDCardComponent> ent, ref AgentIDCardNumberChangedMessage args)
    {
        if (!TryComp<NanoChatCardComponent>(ent, out var comp))
            return;

        _nanoChat.SetNumber((ent, comp), args.Number);
        Dirty(ent, comp);
    }

    [SubscribeLocalEvent]
    private void AfterUIOpen(EntityUid uid, AgentIDCardComponent component, AfterActivatableUIOpenEvent args)
    {
        if (!_uiSystem.HasUi(uid, AgentIDCardUiKey.Key))
            return;

        if (!TryComp<IdCardComponent>(uid, out var idCard))
            return;

        uint? currentNumber = null;
        if (TryComp<NanoChatCardComponent>(uid, out var comp))
            currentNumber = comp.Number;

        var state = new AgentIDCardBoundUserInterfaceState(
            idCard.FullName ?? "",
            idCard.LocalizedJobTitle ?? "",
            idCard.JobIcon,
            currentNumber);

        _uiSystem.SetUiState(uid, AgentIDCardUiKey.Key, state);
    }
    // ADT-Tweak-End
}
