using Content.Shared.ADT.Xenobiology.Potions;
using Robust.Client.UserInterface;

namespace Content.Client.ADT.Xenobiology.UI;

public sealed class SlimeNameChangePotionBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    private SlimeNameChangePotionWindow? _window;

    public SlimeNameChangePotionBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        => IoCManager.InjectDependencies(this);

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<SlimeNameChangePotionWindow>();

        _window.OnNewNameChanged += OnNewNameChanged;
        Reload();
        _window.SetInitialLabelState(); // Must be after Reload() has set the label text
    }

    private void OnNewNameChanged(string newName)
    {
        // Focus moment
        if (_entManager.TryGetComponent(Owner, out SlimeNameChangePotionComponent? component) &&
            component.AssignedName.Equals(newName))
            return;

        SendPredictedMessage(new SlimeNameChangePotionNewNameChangedMessage(newName));
    }

    public void Reload()
    {
        if (_window == null || !_entManager.TryGetComponent(Owner, out SlimeNameChangePotionComponent? component))
            return;

        _window.SetCurrentNewName(component.AssignedName);
    }
}
