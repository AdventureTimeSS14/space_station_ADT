using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.ADT.Holomap;
using Content.Shared.Power;
using Content.Shared.UserInterface;
using Robust.Server.GameObjects;

namespace Content.Server.ADT.Holomap;

public sealed class HolomapSystem : SharedHolomapSystem
{
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HolomapComponent, HolomapModeSelectedMessage>(OnModeSelected);
        SubscribeLocalEvent<HolomapComponent, PowerConsumerReceivedChanged>(OnPowerReceivedChanged);
        SubscribeLocalEvent<HolomapComponent, AnchorStateChangedEvent>(OnAnchorChanged);
    }

    private void OnModeSelected(EntityUid uid, HolomapComponent component, HolomapModeSelectedMessage args)
    {
        SetMode(uid, args.Mode, component);
        UpdateUserInterface(uid, component);
    }

    private void OnPowerReceivedChanged(EntityUid uid, HolomapComponent component, ref PowerConsumerReceivedChanged args)
    {
        var powered = args.ReceivedPower >= args.DrawRate;
        var ev = new PowerChangedEvent(powered, args.ReceivedPower);
        RaiseLocalEvent(uid, ref ev);
    }

    private void OnAnchorChanged(EntityUid uid, HolomapComponent component, ref AnchorStateChangedEvent args)
    {
        if (!TryComp<PowerConsumerComponent>(uid, out var powerConsumer))
            return;

        var powered = args.Anchored && powerConsumer.ReceivedPower >= powerConsumer.DrawRate;
        var ev = new PowerChangedEvent(powered, powerConsumer.ReceivedPower);
        RaiseLocalEvent(uid, ref ev);
    }

    private void UpdateUserInterface(EntityUid uid, HolomapComponent component)
    {
        _uiSystem.SetUiState(uid, HolomapUiKey.Key, new HolomapBoundUserInterfaceState(component.Mode));
    }
}
