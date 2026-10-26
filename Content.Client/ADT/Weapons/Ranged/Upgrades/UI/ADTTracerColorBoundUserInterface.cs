using Content.Shared.ADT.Weapons.Ranged.Upgrades.Components;
using Robust.Client.UserInterface;

namespace Content.Client.ADT.Weapons.Ranged.Upgrades.UI;

public sealed class ADTTracerColorBoundUserInterface : BoundUserInterface
{
    private ADTTracerColorWindow? _window;

    public ADTTracerColorBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<ADTTracerColorWindow>();

        if (EntMan.TryGetComponent<ADTGunUpgradeTracerComponent>(Owner, out var tracer))
            _window.SetColor(tracer.BoltColor);

        _window.OnColorSelected += color => SendMessage(new ADTGunUpgradeTracerColorMessage(color));
    }
}
