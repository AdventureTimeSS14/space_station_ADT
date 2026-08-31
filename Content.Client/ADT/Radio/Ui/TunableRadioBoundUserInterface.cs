using Content.Shared.ADT.Radio;
using Content.Shared.ADT.Radio.Components;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.ADT.Radio.Ui;

[UsedImplicitly]
public sealed class TunableRadioBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private TunableRadioMenu? _menu;

    public TunableRadioBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<TunableRadioMenu>();

        if (EntMan.TryGetComponent(Owner, out ADTTunableRadioComponent? radio))
            _menu.Update((Owner, radio));

        _menu.OnFrequencyChanged += frequency =>
        {
            SendMessage(new ADTTunableRadioSetFrequencyMessage(frequency));
        };

        _menu.OnMicrophonePressed += enabled =>
        {
            SendMessage(new ADTTunableRadioToggleMicrophoneMessage(enabled));
        };

        _menu.OnSpeakerPressed += enabled =>
        {
            SendMessage(new ADTTunableRadioToggleSpeakerMessage(enabled));
        };
    }

    public void Update(Entity<ADTTunableRadioComponent> ent)
    {
        _menu?.Update(ent);
    }
}
