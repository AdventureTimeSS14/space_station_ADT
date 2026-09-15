using Content.Shared.ADT.LogicCircuit;
using JetBrains.Annotations;
using Robust.Client.ResourceManagement;
using Robust.Shared.Prototypes;

namespace Content.Client.ADT.LogicCircuit.UI;

[UsedImplicitly]
public sealed class ADTLogicCircuitBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly IResourceCache _cache = default!;

    private LogicCircuitWindow? _window;

    public ADTLogicCircuitBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = new LogicCircuitWindow(_prototypes, _cache, EntMan.System<ADTLogicCircuitSystem>());
        _window.OnClose += Close;

        _window.OnApplyPressed += layout => SendMessage(new ADTLogicCircuitApplyMessage(layout));
        _window.OnEnabledToggled += enabled => SendMessage(new ADTLogicCircuitSetEnabledMessage(enabled));

        if (State is ADTLogicCircuitBuiState current)
            _window.SetState(current);

        _window.OpenCentered();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is ADTLogicCircuitBuiState current)
            _window?.SetState(current);
    }

    protected override void ReceiveMessage(BoundUserInterfaceMessage message)
    {
        base.ReceiveMessage(message);

        if (message is ADTLogicCircuitValuesMessage values)
            _window?.SetValues(values.PinValues);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
            return;

        _window?.Dispose();
        _window = null;
    }
}
