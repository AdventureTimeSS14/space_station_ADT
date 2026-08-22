using Content.Shared.ADT.Telephone;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.ADT.Telephone;

/// <summary>
/// Client-side telephone window: phone list, call, answer, hang up and DND.
/// </summary>
public sealed class ADTPhoneBui : BoundUserInterface
{
    private ADTPhoneWindow? _window;
    private bool _doNotDisturb;

    public ADTPhoneBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        // Reuse the window if it was somehow opened twice.
        if (_window is { Disposed: false, IsOpen: true })
            return;

        _window = this.CreateWindow<ADTPhoneWindow>();
        if (EntMan.TryGetComponent(Owner, out MetaDataComponent? metaData))
            _window.Title = metaData.EntityName;

        _window.SearchBar.OnTextChanged += OnSearchChanged;
        _window.AnswerButton.OnPressed += _ => SendMessage(new ADTPhoneAnswerMsg());
        _window.HangUpButton.OnPressed += _ => SendMessage(new ADTPhoneHangUpMsg());
        _window.DoNotDisturbButton.OnPressed += _ => SendMessage(new ADTPhoneDoNotDisturbMsg(!_doNotDisturb));

        Refresh();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        Refresh();
    }

    private void OnSearchChanged(LineEdit.LineEditEventArgs args)
    {
        ApplySearchFilter(args.Text);
    }

    private void ApplySearchFilter(string text)
    {
        if (_window == null)
            return;

        foreach (var child in _window.PhonesList.Children)
        {
            if (child is Button button)
                button.Visible = string.IsNullOrEmpty(text) ||
                                 button.Text?.Contains(text, StringComparison.OrdinalIgnoreCase) == true;
        }
    }

    private void Refresh()
    {
        if (_window is not { IsOpen: true } || State is not ADTPhoneBuiState state)
            return;

        _doNotDisturb = state.DoNotDisturb;

        _window.PhonesList.DisposeAllChildren();

        if (state.Phones.Count == 0)
        {
            _window.PhonesList.AddChild(new Label
            {
                Text = Loc.GetString("adt-phone-no-phones"),
                HorizontalAlignment = Control.HAlignment.Center,
                Margin = new Thickness(0, 8),
            });
        }

        foreach (var phone in state.Phones)
        {
            var button = new Button
            {
                Text = phone.Name,
                HorizontalExpand = true,
                StyleClasses = { "OpenBoth" },
            };
            var id = phone.Id;
            button.OnPressed += _ => SendMessage(new ADTPhoneCallMsg(id));
            _window.PhonesList.AddChild(button);
        }

        ApplySearchFilter(_window.SearchBar.Text);

        _window.AnswerButton.Visible = state.Ringing;
        _window.HangUpButton.Visible = state.Engaged;
        _window.DoNotDisturbButton.Text = Loc.GetString(state.DoNotDisturb ? "adt-phone-do-not-disturb-on" : "adt-phone-do-not-disturb-off");
    }
}
