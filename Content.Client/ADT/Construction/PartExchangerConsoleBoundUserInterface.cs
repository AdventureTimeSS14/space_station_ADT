using Content.Client.ADT.Construction.UI;
using Content.Shared.ADT.Construction;
using Content.Shared.Construction.Components;
using Content.Shared.Storage;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client.ADT.Construction;

public sealed class PartExchangerConsoleBoundUserInterface : BoundUserInterface
{
    private PartExchangerConsoleMenu? _menu;
    private bool _sent;

    public PartExchangerConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _sent = false;

        _menu = this.CreateWindow<PartExchangerConsoleMenu>();
        _menu.Title = Loc.GetString("part-exchanger-console-title");
        _menu.OnBoardSelected += OnBoardSelected;

        Populate();

        _menu.OpenCentered();
    }

    private void Populate()
    {
        if (_menu == null)
            return;

        var boards = new List<(NetEntity Board, EntProtoId Console)>();
        if (EntMan.TryGetComponent(Owner, out StorageComponent? storage))
        {
            foreach (var itemUid in storage.Container.ContainedEntities)
            {
                if (!EntMan.TryGetComponent(itemUid, out ComputerBoardComponent? boardComp)
                    || boardComp.Prototype is not { } consoleProto)
                    continue;

                boards.Add((EntMan.GetNetEntity(itemUid), consoleProto));
            }
        }

        _menu.Populate(boards);
    }

    private void OnBoardSelected(NetEntity board)
    {
        if (_sent)
            return;

        _sent = true;

        SendMessage(new PartExchangerConsoleSelectMessage
        {
            Board = board,
        });

        _menu?.Close();
    }
}
