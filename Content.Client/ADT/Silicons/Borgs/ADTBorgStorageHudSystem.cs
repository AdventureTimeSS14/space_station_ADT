using Content.Client.Examine;
using Content.Client.Hands.Systems;
using Content.Client.UserInterface.Controls;
using Content.Client.UserInterface.Systems.Hotbar.Widgets;
using Content.Client.Verbs.UI;
using Content.Shared.ADT.Silicons.Borgs;
using Content.Shared.ADT.Silicons.Borgs.Components;
using Content.Shared.Hands.Components;
using Content.Shared.Input;
using Content.Shared.Interaction;
using Content.Shared.Silicons.Borgs;
using Content.Shared.Silicons.Borgs.Components;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Containers;
using Robust.Shared.Input;
using Robust.Shared.Player;

namespace Content.Client.ADT.Silicons.Borgs;

public sealed class ADTBorgStorageHudSystem : EntitySystem
{
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IUserInterfaceManager _ui = default!;
    [Dependency] private HandsSystem _hands = default!;
    [Dependency] private ExamineSystem _examine = default!;
    [Dependency] private SharedBorgSystem _borg = default!;
    
    private ADTBorgStorageButton? _boundButton;
    private EntityUid? _module;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTBorgStorageModuleComponent, EntGotInsertedIntoContainerMessage>(OnModuleInserted);
        SubscribeLocalEvent<ADTBorgStorageModuleComponent, EntGotRemovedFromContainerMessage>(OnModuleRemoved);
        SubscribeLocalEvent<ADTBorgStorageModuleComponent, ComponentStartup>(OnModuleStartup);
        SubscribeLocalEvent<LocalPlayerAttachedEvent>(_ => UpdateWidget());
        SubscribeLocalEvent<LocalPlayerDetachedEvent>(_ => UpdateWidget());
    }

    private void OnModuleInserted(Entity<ADTBorgStorageModuleComponent> ent, ref EntGotInsertedIntoContainerMessage args)
    {
        OnModuleContainerChanged(args.Container);
    }

    private void OnModuleRemoved(Entity<ADTBorgStorageModuleComponent> ent, ref EntGotRemovedFromContainerMessage args)
    {
        OnModuleContainerChanged(args.Container);
    }

    private void OnModuleContainerChanged(BaseContainer container)
    {
        if (!TryComp<BorgChassisComponent>(container.Owner, out var chassis) ||
            container.ID != chassis.ModuleContainerId)
            return;

        UpdateWidget();
    }

    private void OnModuleStartup(Entity<ADTBorgStorageModuleComponent> ent, ref ComponentStartup args)
    {
        UpdateWidget();
    }

    private void UpdateWidget()
    {
        _module = null;

        if (_ui.GetActiveUIWidgetOrNull<HotbarGui>() is not { } hotbar)
            return;

        var button = hotbar.ADTBorgStorageButton;

        if (_boundButton != button)
        {
            if (_boundButton != null)
                _boundButton.Pressed -= OnPressed;

            button.Pressed += OnPressed;
            _boundButton = button;
        }

        if (_player.LocalEntity is { } player &&
            TryComp<BorgChassisComponent>(player, out var chassis) &&
            _borg.TryGetStorageModule((player, chassis), out var module))
        {
            _module = module;
        }

        if (_module == null)
        {
            button.SetEntity(null);
            button.Visible = false;
            return;
        }

        button.SetEntity(_module);
        button.ToolTip = Loc.GetString("adt-borg-storage-open-tooltip");
        button.Visible = true;
    }

    private void OnPressed(GUIBoundKeyEventArgs args, SlotControl control)
    {
        if (_module is not { } module || !EntityManager.EntityExists(module))
            return;

        if (args.Function == EngineKeyFunctions.UIClick)
        {
            args.Handle();

            if (_player.LocalEntity is { } player &&
                TryComp<HandsComponent>(player, out var hands) &&
                _hands.TryGetActiveItem((player, hands), out var held))
            {
                RaisePredictiveEvent(new ADTBorgStorageInsertHeldRequestEvent());
                return;
            }

            RaisePredictiveEvent(
                new InteractInventorySlotEvent(EntityManager.GetNetEntity(module), altInteract: false));
            return;
        }

        if (args.Function == ContentKeyFunctions.ActivateItemInWorld)
        {
            args.Handle();
            RaisePredictiveEvent(
                new InteractInventorySlotEvent(EntityManager.GetNetEntity(module), altInteract: false));
            return;
        }

        if (args.Function == EngineKeyFunctions.UseSecondary)
        {
            args.Handle();
            _ui.GetUIController<VerbMenuUIController>().OpenVerbMenu(module);
            return;
        }

        if (args.Function == ContentKeyFunctions.ExamineEntity)
        {
            args.Handle();
            _examine.DoExamine(module);
        }
    }
}
