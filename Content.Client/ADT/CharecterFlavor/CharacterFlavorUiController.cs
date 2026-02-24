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
        OpenInternal(target, previewMode: false);
    }

    /// <summary>
    /// Открыть окно флавора в режиме предпросмотра (из лобби, без загрузки headshot с сервера).
    /// </summary>
    public void OpenPreviewMenu(EntityUid target)
    {
        OpenInternal(target, previewMode: true);
    }

    private void OpenInternal(EntityUid target, bool previewMode)
    {
        _window?.Close();
        _window = UIManager.CreateWindow<CharacterFlavorWindow>();
        _window.IsPreviewMode = previewMode;
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

    /// <summary>
    /// Установить хэдшот для окна предпросмотра (из лобби).
    /// Не использует NetEntity, просто берёт текущее preview-окно, если оно открыто.
    /// </summary>
    public void SetPreviewHeadshot(byte[] image)
    {
        if (_window == null || !_window.IsPreviewMode)
            return;

        _window.SetHeadshot(image);
    }
}
