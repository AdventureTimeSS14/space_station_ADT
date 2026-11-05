using Content.Shared.ADT.Cooking;
using Content.Shared.ADT.Cooking.Components;
using Robust.Client.UserInterface;

namespace Content.Client.ADT.Cooking.UI;

public sealed class ADTStoveBoundUserInterface : BoundUserInterface
{
    private ADTStoveWindow? _window;

    public ADTStoveBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<ADTStoveWindow>();
        _window.Title = EntMan.GetComponent<MetaDataComponent>(Owner).EntityName;

        if (EntMan.TryGetComponent<ADTStoveComponent>(Owner, out var stove))
            _window.Setup(Owner, stove);

        _window.OnTimerSelected += seconds => SendMessage(new ADTStoveSetTimerMessage(seconds));
    }
}
