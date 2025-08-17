using Robust.Client.UserInterface;
using JetBrains.Annotations;
using Content.Shared.ADT.Ghost;
using Content.Shared.CrewManifest;

namespace Content.Client.ADT.Ghost;

[UsedImplicitly]
public sealed class GhostInfoBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private GhostInfo? _window;

    /// <summary>
    /// Инициализирует новый экземпляр GhostInfoBoundUserInterface для указанной сущности и ключа интерфейса.
    /// </summary>
    /// <param name="owner">Идентификатор сущности, которой принадлежит интерфейс.</param>
    /// <param name="uiKey">Ключ пользовательского интерфейса, определяющий конкретный экземпляр/тип UI.</param>
    public GhostInfoBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    /// <summary>
    /// Открывает пользовательский интерфейс и инициализирует окно GhostInfo.
    /// </summary>
    /// <remarks>
    /// Вызывает базовую реализацию <c>Open</c> и создаёт окно <c>GhostInfo</c>, сохраняемое в поле <c>_window</c>.
    /// </remarks>
    protected override void Open()
    {

        base.Open();

        _window = this.CreateWindow<GhostInfo>();
    }

    /// <summary>
    /// Обрабатывает обновления состояния интерфейса: вызывает базовую обработку и, если полученное состояние — <see cref="GhostInfoUpdateState"/> и окно создано, перенаправляет его в окно.
    /// </summary>
    /// <param name="state">Состояние интерфейса; только экземпляры <see cref="GhostInfoUpdateState"/> будут переданы в окно.</param>
    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not GhostInfoUpdateState updateState || _window == null)
            return;

        _window.UpdateState(updateState);
    }
}
