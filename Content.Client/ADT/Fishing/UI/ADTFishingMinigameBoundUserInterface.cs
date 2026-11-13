using Content.Shared.ADT.Fishing;
using Robust.Client.UserInterface;

namespace Content.Client.ADT.Fishing.UI;

public sealed class ADTFishingMinigameBoundUserInterface : BoundUserInterface
{
    private ADTFishingMinigameWindow? _window;

    public ADTFishingMinigameBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<ADTFishingMinigameWindow>();
        _window.SetRod(Owner);
        _window.OnHold += holding => SendMessage(new ADTFishingHoldMessage(holding));
    }
}
