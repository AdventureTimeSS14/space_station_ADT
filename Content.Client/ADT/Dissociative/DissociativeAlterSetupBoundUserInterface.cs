using Content.Shared.ADT.Dissociative;
using Robust.Client.UserInterface;

namespace Content.Client.ADT.Dissociative;

public sealed class DissociativeAlterSetupBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private DissociativeAlterSetupWindow? _window;

    public DissociativeAlterSetupBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<DissociativeAlterSetupWindow>();
        _window.OnSexSelected += sex => SendMessage(new DissociativeAlterSetSexMessage(sex));
        _window.OnCustomize += () => SendMessage(new DissociativeAlterOpenCustomizationMessage());
        _window.OnConfirm += name => SendMessage(new DissociativeAlterConfirmMessage(name));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is DissociativeAlterSetupUiState cast)
            _window?.UpdateState(cast.Name, cast.Sex);
    }
}
