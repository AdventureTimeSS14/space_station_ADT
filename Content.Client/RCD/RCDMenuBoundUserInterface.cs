using Content.Shared.ADT.CCVar; // ADT Radial menu settings
using Content.Shared.RCD;
using Content.Shared.RCD.Components;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.UserInterface;
using Robust.Shared.Configuration; // ADT Radial menu settings
using Robust.Shared.Prototypes;

namespace Content.Client.RCD;

[UsedImplicitly]
public sealed class RCDMenuBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IClyde _displayManager = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!; // ADT Radial menu settings

    private RCDMenu? _menu;

    public RCDMenuBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<RCDMenu>();
        _menu.SetEntity(Owner);
        _menu.SendRCDSystemMessageAction += SendRCDSystemMessage;

        // Open the menu, centered on the mouse
        var vpSize = _displayManager.ScreenSize;
        // ADT Radial menu settings start
        if (_cfg.GetCVar(ADTCCVars.CenterRadialMenu) == false)
            _menu.OpenCenteredAt(_inputManager.MouseScreenPosition.Position / vpSize);
        else
            _menu.OpenCentered();
        // ADT Radial menu settings end
    }

    public void SendRCDSystemMessage(ProtoId<RCDPrototype> protoId)
    {
        // A predicted message cannot be used here as the RCD UI is closed immediately
        // after this message is sent, which will stop the server from receiving it
        SendMessage(new RCDSystemMessage(protoId));
    }
}
