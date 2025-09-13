using Content.Shared.ADT.Storage.Components;
using Robust.Client.UserInterface;
using Content.Client.UserInterface.Controls;
using Content.Client.ADT.Storage.SuitStorage.UI;

namespace Content.Client.ADT.Storage.UI;

public sealed class SuitStorageBoundUserInterface : BoundUserInterface
{
    private SuitStorageMenu? _menu;

    public SuitStorageBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<SuitStorageMenu>();
        _menu.SetEntity(Owner);
        _menu.OnSelected += i =>
        {
            SendMessage(new SuitStorageMessage(i));
            Close();
        };
    }
}