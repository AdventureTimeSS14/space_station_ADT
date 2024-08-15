using JetBrains.Annotations;

namespace Content.Client.ADT.Economy.UI;

[UsedImplicitly]
public sealed class EftposBui : BoundUserInterface
{
    private readonly EftposWindow _window;

    public EftposBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _window = new EftposWindow();
    }

    protected override void Open()
    {
        base.Open();
        _window.OnClose += Close;
        _window.OnCardButtonPressed += SendMessage;

        if (State != null)
        {
            UpdateState(State);
        }

        _window.OpenCentered();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        _window.UpdateState(state);
    }

    protected override void Dispose(bool disposing)
    {
        _window.Close();
        base.Dispose(disposing);
    }
}
