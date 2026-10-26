using Content.Shared.ADT.GPS;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.ADT.GPS.UI;

[UsedImplicitly]
public sealed class GpsBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private GpsWindow? _window;

    public GpsBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<GpsWindow>();
        _window.OnTogglePressed += () => SendMessage(new GpsToggleMessage());
        _window.OnRangePressed += () => SendMessage(new GpsToggleRangeMessage());
        _window.OnTagEntered += tag => SendMessage(new GpsSetTagMessage(tag));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not GpsBoundUserInterfaceState cast)
            return;

        _window?.UpdateState(cast);
    }
}
