using JetBrains.Annotations;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Prototypes;
using Content.Client.ADT.Phantom.UI;
using Content.Shared.ADT.Phantom;
using Robust.Shared.Player;
using Content.Shared.Preferences;

namespace Content.Client.UserInterface.Systems.Phantom;

[UsedImplicitly]
public sealed class RadioFakerUIController : UIController
{
    [Dependency] private readonly IEntityManager _entityManager = default!;

    private RadioFakerMenu? _menu;

    public void OpenMenu(NetEntity user, NetEntity target, List<string> channels)
    {
        _menu?.Close();

        _menu = UIManager.CreateWindow<RadioFakerMenu>();
        _menu.Populate(channels);
        _menu.OnSend += (channel, name, message) => Send(user, target, channel, name, message);
        _menu.OpenCentered();
    }

    public void CloseMenu()
    {
        _menu?.Close();
    }

    private void Send(NetEntity user, NetEntity target, string channel, string name, string message)
    {
        var ev = new SendRadioFakerMessageEvent(user, target, message, name, channel);
        _entityManager.RaisePredictiveEvent(ev);

        CloseMenu();
    }
}
