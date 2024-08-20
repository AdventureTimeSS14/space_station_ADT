using Content.Shared.ADT.CCVar; // ADT Radial menu settings
using Content.Shared.ADT.RPD;
using Content.Shared.ADT.RPD.Components;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Shared.Configuration; // ADT Radial menu settings
using Robust.Shared.Prototypes;

namespace Content.Client.ADT.RPD;

[UsedImplicitly]
public sealed class RPDMenuBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IClyde _displayManager = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!; // ADT Radial menu settings
    private RPDMenu? _menu;

    public RPDMenuBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Open()
    {
        base.Open();

        _menu = new(Owner, this);
        _menu.OnClose += Close;

        // Open the menu, centered on the mouse
        var vpSize = _displayManager.ScreenSize;
        // ADT Radial menu settings start
        if (_cfg.GetCVar(ADTCCVars.CenterRadialMenu) == false)
            _menu.OpenCenteredAt(_inputManager.MouseScreenPosition.Position / vpSize);
        else
            _menu.OpenCentered();
        // ADT Radial menu settings end
    }

    public void SendRPDSystemMessage(ProtoId<RPDPrototype> protoId)
    {
        // A predicted message cannot be used here as the RPD UI is closed immediately
        // after this message is sent, which will stop the server from receiving it
        SendMessage(new RPDSystemMessage(protoId));
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing) return;

        _menu?.Dispose();
    }
}
