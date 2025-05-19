using JetBrains.Annotations;

namespace Content.Client.ADT.Minesweeper.Minesweeper;

[UsedImplicitly]
public sealed class MinesweeperBoundUserInterface : BoundUserInterface
{
    private MinesweeperWindow? _window;
    private EntityUid _owner;

    public MinesweeperBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
        _owner = owner;
    }

    protected override void Open()
    {
        base.Open();

        _window = new MinesweeperWindow();

        _window.OpenCentered();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _window?.Close();
    }
}
