using Content.Shared.VoiceMask;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client.VoiceMask;

public sealed class VoiceMaskBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IPrototypeManager _protomanager = default!;

    [ViewVariables]
    private VoiceMaskNameChangeWindow? _window;

    public VoiceMaskBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<VoiceMaskNameChangeWindow>();
        _window.ReloadVerbs(_protomanager);
        _window.AddVerbs();

        _window.OnNameChange += OnNameSelected;
        _window.OnVerbChange += verb => SendMessage(new VoiceMaskChangeVerbMessage(verb));
        _window.OnVoiceChange += voice => SendMessage(new VoiceMaskChangeVoiceMessage(voice)); // Corvax-TTS
        // _window.OnBarkChange += bark => SendMessage(new VoiceMaskChangeBarkMessage(bark)); // ADT Barks
        // _window.OnPitchChange += pitch => SendMessage(new VoiceMaskChangeBarkPitchMessage(pitch)); // ADT Barks
    }

    private void OnNameSelected(string name)
    {
        SendMessage(new VoiceMaskChangeNameMessage(name));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not VoiceMaskBuiState cast || _window == null)
        {
            return;
        }
        //  _window.UpdateState(cast.Name, cast.Voice, cast.Bark, cast.Pitch, cast.Verb); // Corvax-TTS && ADT Barks
        _window.UpdateState(cast.Name, cast.Voice, cast.Verb); // Corvax-TTS && ADT Barks
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        _window?.Close();
    }
}
