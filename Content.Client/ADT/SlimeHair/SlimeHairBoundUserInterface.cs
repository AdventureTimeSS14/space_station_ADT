using Content.Shared.Humanoid.Markings;
using Content.Shared.ADT.SlimeHair;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;

namespace Content.Client.ADT.SlimeHair;

public sealed class SlimeHairBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private SlimeHairWindow? _window;

    public SlimeHairBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<SlimeHairWindow>();

        _window.OnHairSelected += tuple => SelectHair(SlimeHairCategory.Hair, tuple.id, tuple.slot);
        _window.OnHairColorChanged += args => ChangeColor(SlimeHairCategory.Hair, args.marking, args.slot);
        _window.OnHairSlotAdded += delegate () { AddSlot(SlimeHairCategory.Hair); };
        _window.OnHairSlotRemoved += args => RemoveSlot(SlimeHairCategory.Hair, args);

        _window.OnFacialHairSelected += tuple => SelectHair(SlimeHairCategory.FacialHair, tuple.id, tuple.slot);
        _window.OnFacialHairColorChanged +=
            args => ChangeColor(SlimeHairCategory.FacialHair, args.marking, args.slot);
        _window.OnFacialHairSlotAdded += delegate () { AddSlot(SlimeHairCategory.FacialHair); };
        _window.OnFacialHairSlotRemoved += args => RemoveSlot(SlimeHairCategory.FacialHair, args);
    }

    private void SelectHair(SlimeHairCategory category, string marking, int slot)
    {
        SendMessage(new SlimeHairSelectMessage(category, marking, slot));
    }

    private void ChangeColor(SlimeHairCategory category, Marking marking, int slot)
    {
        SendMessage(new SlimeHairChangeColorMessage(category, new(marking.MarkingColors), slot));
    }

    private void RemoveSlot(SlimeHairCategory category, int slot)
    {
        SendMessage(new SlimeHairRemoveSlotMessage(category, slot));
    }

    private void AddSlot(SlimeHairCategory category)
    {
        SendMessage(new SlimeHairAddSlotMessage(category));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not SlimeHairUiState data || _window == null)
        {
            return;
        }

        _window.UpdateState(data);
    }
}
