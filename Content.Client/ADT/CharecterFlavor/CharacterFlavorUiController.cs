using Robust.Client.UserInterface;
using JetBrains.Annotations;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.GameStates;
using Content.Client.Gameplay;

namespace Content.Client.ADT.CharecterFlavor;

[UsedImplicitly]
public sealed class CharacterFlavorUiController : UIController, IOnStateEntered<GameplayState>
{
    [ViewVariables]
    private CharacterFlavorWindow? _window;

    private NetEntity _currentEntity;

    public void OnStateEntered(GameplayState state)
    {
        _window?.Close();
        _window = null;
    }

    public void OpenMenu(EntityUid target)
    {
        _window?.Close();
        _window = UIManager.CreateWindow<CharacterFlavorWindow>();
        _window.SetEntity(target);
        _window.OpenCentered();

        _currentEntity = EntityManager.GetNetEntity(target);
    }

    public void SetHeadshot(NetEntity target, byte[] image)
    {
        if (target != _currentEntity)
            return;

        _window?.SetHeadshot(image);
    }
}
