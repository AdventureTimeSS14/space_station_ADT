using Content.Shared.ADT.SeedDna;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Research.Components;
using static Content.Shared.ADT.SeedDna.Components.SeedDnaConsoleComponent;

namespace Content.Client.ADT.SeedDna.UI;

/// <summary>
/// Контейнер передачи данных от сервера к клиентскому UI.
/// </summary>
public sealed class SeedDnaConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private SeedDnaConsoleWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = new SeedDnaConsoleWindow();

        _window.SeedButton.OnPressed += _ => SendMessage(new ItemSlotButtonPressedEvent(SeedSlotId));
        _window.DnaDiskButton.OnPressed += _ => SendMessage(new ItemSlotButtonPressedEvent(DnaDiskSlotId));

        _window.ServerListButton.OnPressed += _ => SendMessage(new ConsoleServerSelectionMessage());
        _window.OnGeneTransfer += (geneId, direction) =>
            SendMessage(new SeedDnaGeneTransferMessage(geneId, direction));
        _window.SellButton.OnPressed += _ => SendMessage(new SeedDnaSellMessage());

        _window.OnClose += Close;
        _window.OpenCentered();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
            return;

        _window?.Close();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        var castState = (SeedDnaConsoleBoundUserInterfaceState)state;
        _window?.UpdateState(castState);
    }
}
