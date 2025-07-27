using JetBrains.Annotations;
using Content.Client.Popups;
using Content.Client.UserInterface.Controls;
using Content.Shared.ADT.CCVar;
using Content.Shared.ADT.RPD;
using Content.Shared.ADT.RPD.Components;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.Input;
using Robust.Shared.Configuration;
using Robust.Shared.Collections;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.ADT.RPD;

[UsedImplicitly]
public sealed class RPDMenuBoundUserInterface : BoundUserInterface
{
    private const string TopLevelActionCategory = "Main";

    private static readonly Dictionary<string, (string Tooltip, SpriteSpecifier Sprite)> PrototypesGroupingInfo
        = new Dictionary<string, (string Tooltip, SpriteSpecifier Sprite)>
        {
            ["DisposalPipe"] = ("rpd-component-DisposalPipe", new SpriteSpecifier.Texture(new ResPath("/Textures/ADT/Interface/Radial/RPD/DisposalPipe.png"))),
            ["Gaspipes"] = ("rpd-component-Gaspipes", new SpriteSpecifier.Texture(new ResPath("/Textures/ADT/Interface/Radial/RPD/Gaspipes.png"))),
            ["Devices"] = ("rpd-component-Devices", new SpriteSpecifier.Texture(new ResPath("/Textures/ADT/Interface/Radial/RPD/Devices.png"))),
        };

    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly ISharedPlayerManager _playerManager = default!;
    [Dependency] private readonly IInputManager _inputManager = default!; // ADT Radial menu settings
    [Dependency] private readonly IClyde _displayManager = default!; // ADT Radial menu settings
    [Dependency] private readonly IConfigurationManager _cfg = default!; // ADT Radial menu settings

    private SimpleRadialMenu? _menu;

    public RPDMenuBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Open()
    {
        base.Open();

        if (!EntMan.TryGetComponent<RPDComponent>(Owner, out var rpd))
            return;

        _menu = this.CreateWindow<SimpleRadialMenu>();
        _menu.Track(Owner);
        var models = ConvertToButtons(rpd.AvailablePrototypes);
        _menu.SetButtons(models);

        // ADT Radial menu settings start
        var vpSize = _displayManager.ScreenSize;

        if (_cfg.GetCVar(ADTCCVars.CenterRadialMenu) == false)
            _menu.OpenCenteredAt(_inputManager.MouseScreenPosition.Position / vpSize);
        else
            _menu.OpenCentered();
        // ADT Radial menu settings end
    }

    private IEnumerable<RadialMenuOption> ConvertToButtons(HashSet<ProtoId<RPDPrototype>> prototypes)
    {
        Dictionary<string, List<RadialMenuActionOption>> buttonsByCategory = new();
        ValueList<RadialMenuActionOption> topLevelActions = new();

        foreach (var protoId in prototypes)
        {
            var prototype = _prototypeManager.Index(protoId);
            if (prototype.Category == TopLevelActionCategory)
            {
                var topLevelActionOption = new RadialMenuActionOption<RPDPrototype>(HandleMenuOptionClick, prototype)
                {
                    Sprite = prototype.Sprite,
                    ToolTip = GetTooltip(prototype)
                };
                topLevelActions.Add(topLevelActionOption);
                continue;
            }

            if (!PrototypesGroupingInfo.TryGetValue(prototype.Category, out var groupInfo))
                continue;

            if (!buttonsByCategory.TryGetValue(prototype.Category, out var list))
            {
                list = new List<RadialMenuActionOption>();
                buttonsByCategory.Add(prototype.Category, list);
            }

            var actionOption = new RadialMenuActionOption<RPDPrototype>(HandleMenuOptionClick, prototype)
            {
                Sprite = prototype.Sprite,
                ToolTip = GetTooltip(prototype)
            };
            list.Add(actionOption);
        }

        var models = new RadialMenuOption[buttonsByCategory.Count + topLevelActions.Count];
        var i = 0;
        foreach (var (key, list) in buttonsByCategory)
        {
            var groupInfo = PrototypesGroupingInfo[key];
            models[i] = new RadialMenuNestedLayerOption(list)
            {
                Sprite = groupInfo.Sprite,
                ToolTip = Loc.GetString(groupInfo.Tooltip)
            };
            i++;
        }

        foreach (var action in topLevelActions)
        {
            models[i] = action;
            i++;
        }

        return models;
    }

    private void HandleMenuOptionClick(RPDPrototype proto)
    {
        // A predicted message cannot be used here as the RPD UI is closed immediately
        // after this message is sent, which will stop the server from receiving it
        SendMessage(new RPDSystemMessage(proto.ID));


        if (_playerManager.LocalSession?.AttachedEntity == null)
            return;

        var msg = Loc.GetString("rpd-component-change-mode", ("mode", Loc.GetString(proto.SetName)));

        if (proto.Mode is RpdMode.ConstructObject)
        {
            var name = Loc.GetString(proto.SetName);

            if (proto.Prototype != null &&
                _prototypeManager.TryIndex(proto.Prototype, out var entProto, logError: false))
                name = entProto.Name;

            msg = Loc.GetString("rpd-component-change-build-mode", ("name", name));
        }

        // Popup message
        var popup = EntMan.System<PopupSystem>();
        popup.PopupClient(msg, Owner, _playerManager.LocalSession.AttachedEntity);
    }

    private string GetTooltip(RPDPrototype proto)
    {
        string tooltip;

        if (proto.Mode is RpdMode.ConstructObject
            && proto.Prototype != null
            && _prototypeManager.TryIndex(proto.Prototype, out var entProto, logError: false))
        {
            tooltip = Loc.GetString(entProto.Name);
        }
        else
        {
            tooltip = Loc.GetString(proto.SetName);
        }

        tooltip = OopsConcat(char.ToUpper(tooltip[0]).ToString(), tooltip.Remove(0, 1));

        return tooltip;
    }

    private static string OopsConcat(string a, string b)
    {
        // This exists to prevent Roslyn being clever and compiling something that fails sandbox checks.
        return a + b;
    }
}
