using Robust.Client.UserInterface;

namespace Content.Client.ADT.AshWalker.UI;

public sealed class ADTCookingScrollBoundUserInterface : BoundUserInterface
{
    public ADTCookingScrollBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        this.CreateWindow<ADTCookingScrollWindow>();
    }
}
