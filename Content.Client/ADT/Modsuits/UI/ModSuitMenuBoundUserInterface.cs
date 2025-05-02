using Content.Shared.ADT.ModSuits;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.UserInterface;

namespace Content.Client.ADT.Modsuits.UI;

public sealed class ModSuitMenuBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IClyde _displayManager = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;

    private IEntityManager _entityManager;
    private ModSuitSystem _modsuit;
    private ModSuitMenu? _menu;

    public ModSuitMenuBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
        _entityManager = IoCManager.Resolve<IEntityManager>();
        _modsuit = EntMan.System<ModSuitSystem>();
    }
    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not ModBoundUiState msg)
            return;
        _menu?.UpdateModStats();
        _menu?.UpdateModuleView(msg);

    }
    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<ModSuitMenu>();
        _menu.SetEntity(Owner);

        _menu.OpenCentered();
        _menu.UpdateModStats();

        _menu.LockButton.OnPressed += _ => OnLockPressed();

        _menu.OnRemoveButtonPressed += Owner =>
        {
            SendMessage(new ModModuleRemoveMessage(EntMan.GetNetEntity(Owner)));
        };
        _menu.OnActivateButtonPressed += Owner =>
        {
            SendMessage(new ModModulActivateMessage(EntMan.GetNetEntity(Owner)));
        };
        _menu.OnDeactivateButtonPressed += Owner =>
        {
            SendMessage(new ModModulDeactivateMessage(EntMan.GetNetEntity(Owner)));
        };
    }
    private void OnLockPressed()
    {
        var msg = new ModLockMessage(EntMan.GetNetEntity(Owner));
        SendMessage(msg);
        _menu?.UpdateModStats();
    }
}
