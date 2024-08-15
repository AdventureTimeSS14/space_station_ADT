using JetBrains.Annotations;

namespace Content.Client.ADT.Economy.UI;


[UsedImplicitly]
public sealed class ATMBui : BoundUserInterface
{
    private AtmWindow _window;

    public ATMBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _window = new AtmWindow();
    }

    protected override void Open()
    {
        base.Open();
        _window.OnClose += Close;
        _window.OnWithdrawAttempt += SendMessage;

        if (State != null)
        {
            UpdateState(State);
        }

        _window.OpenCentered();

    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        _window?.UpdateState(state);
    }

    protected override void Dispose(bool disposing)
    {
        _window?.Close();
        base.Dispose(disposing);
    }
}
