using Content.Shared.ADT.Heretic.Components;
using Content.Shared.ADT.Heretic.Messages;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client.ADT.Heretic.BUI;

public sealed class MawedCrucibleBoundUserInterface : BoundUserInterface
{
    private readonly IPrototypeManager _proto;

    private MawedCrucibleMenu? _menu;

    public MawedCrucibleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _proto = IoCManager.Resolve<IPrototypeManager>();
    }

    protected override void Open()
    {
        base.Open();

        if (!EntMan.TryGetComponent<MawedCrucibleComponent>(Owner, out var comp))
            return;

        _menu = new MawedCrucibleMenu(comp.Potions, _proto);
        _menu.OnPotionSelected += OnPotionSelected;
        _menu.OnClose += Close;
        _menu.OpenCentered();
    }

    private void OnPotionSelected(EntProtoId proto)
    {
        SendPredictedMessage(new MawedCrucibleMessage(proto));
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _menu?.Dispose();
        }
    }
}
