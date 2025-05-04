using Content.Shared.Humanoid.Markings;
using Content.Shared.ADT.IpcScreen;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;

namespace Content.Client.ADT.IpcScreen;

public sealed class IpcScreenBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private IpcScreenWindow? _window;

    public IpcScreenBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<IpcScreenWindow>();

        _window.OnFacialHairSelected += tuple => SelectHair(IpcScreenCategory.FacialHair, tuple.id, tuple.slot);
        _window.OnFacialHairColorChanged +=
            args => ChangeColor(IpcScreenCategory.FacialHair, args.marking, args.slot);
        _window.OnFacialHairSlotAdded += delegate () { AddSlot(IpcScreenCategory.FacialHair); };
        _window.OnFacialHairSlotRemoved += args => RemoveSlot(IpcScreenCategory.FacialHair, args);
    }

    private void SelectHair(IpcScreenCategory category, string marking, int slot)
    {
        SendMessage(new IpcScreenSelectMessage(category, marking, slot));
    }

    private void ChangeColor(IpcScreenCategory category, Marking marking, int slot)
    {
        SendMessage(new IpcScreenChangeColorMessage(category, new(marking.MarkingColors), slot));
    }

    private void RemoveSlot(IpcScreenCategory category, int slot)
    {
        SendMessage(new IpcScreenRemoveSlotMessage(category, slot));
    }

    private void AddSlot(IpcScreenCategory category)
    {
        SendMessage(new IpcScreenAddSlotMessage(category));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not IpcScreenUiState data || _window == null)
        {
            return;
        }

        _window.UpdateState(data);
    }
}
