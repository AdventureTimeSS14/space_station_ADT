using Content.Client.Gameplay;
using Content.Shared.ADT.Language;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;
using Content.Client.UserInterface.Controls;
using Content.Shared.Input;
using Content.Client.ADT.Language.UI;
using Robust.Shared.Input.Binding;
using Robust.Client.Player;
using Content.Client.ADT.Language;
using System.Linq;

namespace Content.Client.UserInterface.Systems.Language;    // ADT Languages

public sealed class LanguageMenuUIController : UIController, IOnStateEntered<GameplayState>, IOnStateExited<GameplayState>
{
    public LanguageMenuWindow? _menu;
    private MenuButton? LanguagesButton => UIManager.GetActiveUIWidgetOrNull<MenuBar.Widgets.GameTopMenuBar>()?.LanguagesButton;

    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IEntityManager _entMan = default!;
    public override void Initialize()
    {

        EntityManager.EventBus.SubscribeEvent<LanguageMenuStateMessage>(EventSource.All, this, OnStateUpdate);
    }

    private void OnStateUpdate(LanguageMenuStateMessage ev)
    {
        if (_menu == null)
            return;

        _menu.UpdateState(ev.CurrentLanguage, ev.Options, ev.TranslatorOptions);
    }


    public void OnStateEntered(GameplayState state)
    {
        CommandBinds.Builder
            .Bind(ContentKeyFunctions.OpenLanguagesMenu,
                InputCmdHandler.FromDelegate(_ => ToggleLanguagesMenu()))
            .Register<LanguageMenuUIController>();
    }

    public void OnStateExited(GameplayState state)
    {
        CommandBinds.Unregister<LanguageMenuUIController>();
    }

    private void ToggleLanguagesMenu()
    {
        var player = _player.LocalEntity;
        if (!player.HasValue)
            return;

        if (_menu == null)
        {
            if (!_entMan.System<LanguageSystem>().GetLanguages(player, out var understood, out _, out var translatorUnderstood, out _, out var current))
                return;

            // setup window
            _menu = UIManager.CreateWindow<LanguageMenuWindow>();
            _menu.Owner = player.Value;
            _menu.UpdateState(current, understood.ToList(), translatorUnderstood.ToList());
            _menu.OnClose += OnWindowClosed;
            _menu.OnOpen += OnWindowOpen;
            _menu.OnLanguageSelected += OnLanguageSelected;

            if (LanguagesButton != null)
                LanguagesButton.SetClickPressed(true);


            _menu.OpenCentered();
        }
        else
        {
            _menu.OnClose -= OnWindowClosed;
            _menu.OnOpen -= OnWindowOpen;
            //_menu.OnPlayEmote -= OnPlayEmote;

            if (LanguagesButton != null)
                LanguagesButton.SetClickPressed(false);

            CloseMenu();
        }
    }

    private void OnLanguageSelected(string lang)
    {
        var player = _player.LocalEntity;
        if (!player.HasValue)
            return;

        var ev = new LanguageChosenMessage(_entMan.GetNetEntity(player.Value), lang);
        _entMan.RaisePredictiveEvent(ev);
    }

    public void UnloadButton()
    {
        if (LanguagesButton == null)
            return;

        LanguagesButton.OnPressed -= ActionButtonPressed;
    }

    public void LoadButton()
    {
        if (LanguagesButton == null)
            return;

        LanguagesButton.OnPressed += ActionButtonPressed;
    }

    private void ActionButtonPressed(BaseButton.ButtonEventArgs args)
    {
        ToggleLanguagesMenu();
    }

    private void OnWindowClosed()
    {
        if (LanguagesButton != null)
            LanguagesButton.Pressed = false;

        CloseMenu();
    }

    private void OnWindowOpen()
    {
        if (LanguagesButton != null)
            LanguagesButton.Pressed = true;
    }

    private void CloseMenu()
    {
        if (_menu == null)
            return;

        _menu.Dispose();
        _menu = null;
    }
}
