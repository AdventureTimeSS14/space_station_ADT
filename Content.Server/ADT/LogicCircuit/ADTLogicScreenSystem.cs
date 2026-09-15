using Content.Server.ADT.LogicCircuit.Components;
using Content.Server.DeviceLinking.Systems;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.DeviceNetwork;
using Content.Shared.TextScreen;

namespace Content.Server.ADT.LogicCircuit;

public sealed class ADTLogicScreenSystem : EntitySystem
{
    [Dependency] private readonly DeviceLinkSystem _deviceLink = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTLogicScreenComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<ADTLogicScreenComponent, SignalReceivedEvent>(OnSignalReceived);
    }

    private void OnInit(Entity<ADTLogicScreenComponent> ent, ref ComponentInit args)
    {
        _deviceLink.EnsureSinkPorts(ent, ent.Comp.TextPort, ent.Comp.ColorPort);
    }

    private void OnSignalReceived(Entity<ADTLogicScreenComponent> ent, ref SignalReceivedEvent args)
    {
        if (args.Data == null
            || !args.Data.TryGetValue(ADTLogicCircuitSystem.LogicValueKey, out string? value)
            || value == null)
        {
            return;
        }

        if (args.Port == ent.Comp.TextPort.Id)
        {
            SetText(ent, value);
            return;
        }

        if (args.Port == ent.Comp.ColorPort.Id)
            SetColor(ent, value);
    }

    private void SetText(Entity<ADTLogicScreenComponent> ent, string text)
    {
        if (text.Length > ent.Comp.MaxLength)
            text = text[..ent.Comp.MaxLength];

        _appearance.SetData(ent, TextScreenVisuals.DefaultText, text);
        _appearance.SetData(ent, TextScreenVisuals.ScreenText, text);
    }

    private void SetColor(Entity<ADTLogicScreenComponent> ent, string value)
    {
        if (Color.TryFromHex(value) is not { } color)
            return;

        _appearance.SetData(ent, TextScreenVisuals.Color, color);
    }
}
