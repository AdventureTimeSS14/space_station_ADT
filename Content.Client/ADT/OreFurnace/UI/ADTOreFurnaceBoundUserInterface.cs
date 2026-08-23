using Content.Shared.ADT.OreFurnace;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.ADT.OreFurnace.UI;

[UsedImplicitly]
public sealed class ADTOreFurnaceBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private ADTOreFurnaceWindow? _window;

    public ADTOreFurnaceBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindowCenteredRight<ADTOreFurnaceWindow>();
        _window.SetEntity(Owner);

        _window.OnSmelt += (recipe, amount) => SendMessage(new ADTOreFurnaceSmeltMessage(recipe, amount));
        _window.OnSmeltEverything += () => SendMessage(new ADTOreFurnaceSmeltAllMessage());
        _window.OnClaimPoints += () => SendMessage(new ADTOreFurnaceClaimPointsMessage());
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not ADTOreFurnaceUpdateState furnaceState)
            return;

        _window?.Update(furnaceState.Points, furnaceState.CanClaim);
    }
}
