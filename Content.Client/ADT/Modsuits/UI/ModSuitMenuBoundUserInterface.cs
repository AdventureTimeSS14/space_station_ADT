using Content.Shared.ADT.ModSuits;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.UserInterface;

namespace Content.Client.ADT.Modsuits.UI;

public sealed class ModSuitMenuBoundUserInterface : BoundUserInterface
{
    private ModSuitMenu? _menu;

    public ModSuitMenuBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
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

        _menu.OnRemoveButtonPressed += owner => SendPredictedMessage(new ModModuleRemoveMessage(EntMan.GetNetEntity(owner)));
        _menu.OnActivateButtonPressed += owner => SendPredictedMessage(new ModModuleActivateMessage(EntMan.GetNetEntity(owner)));
        _menu.OnDeactivateButtonPressed += owner => SendPredictedMessage(new ModModuleDeactivateMessage(EntMan.GetNetEntity(owner)));
    }
    private void OnLockPressed()
    {
        var msg = new ModLockMessage(EntMan.GetNetEntity(Owner));
        SendPredictedMessage(msg);
        _menu?.UpdateModStats();
    }
}
