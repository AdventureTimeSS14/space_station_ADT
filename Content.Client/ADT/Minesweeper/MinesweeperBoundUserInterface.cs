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

        // TODO: Запись рекордов
        // if (EntMan.TryGetComponent<MinesweeperComponent>(_owner, out var minesweeper) && EntMan.TryGetComponent<MetaDataComponent>(_owner, out var metaData))
        // {
        //     _window.LoadRecords(minesweeper.Records, _owner, minesweeper, this);
        // }

        _window.OpenCentered();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _window?.Close();
    }
}
