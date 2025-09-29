using Content.Shared.ADT.ModSuits;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.UserInterface;

namespace Content.Client.ADT.Modsuits.UI;

public sealed class ModSuitRadialBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IClyde _displayManager = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;

    private IEntityManager _entityManager;
    private ModSuitRadialMenu? _menu;

    public ModSuitRadialBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
        _entityManager = IoCManager.Resolve<IEntityManager>();
    }

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<ModSuitRadialMenu>();
        _menu.SetEntity(Owner);

        _menu.SendToggleClothingMessageAction += SendModSuitMessage;
        _menu.OpenCentered();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not RadialModBoundUiState)
            return;

        _menu?.RefreshUI();
    }

    private void SendModSuitMessage(EntityUid uid)
    {
        var message = new ToggleModSuitPartMessage(_entityManager.GetNetEntity(uid));
        SendPredictedMessage(message);
    }
}
