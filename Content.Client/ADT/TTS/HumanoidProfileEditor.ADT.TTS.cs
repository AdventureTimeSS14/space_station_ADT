using Content.Client.ADT.TTS;
using Content.Shared.Preferences;
using Robust.Client.UserInterface.CustomControls;
using System.Numerics;

namespace Content.Client.Lobby.UI;

/// <summary>
/// ADT: Extension for HumanoidProfileEditor to add TTS voice selection window
/// </summary>
public sealed partial class HumanoidProfileEditor
{
    private DefaultWindow? _adtTtsWindow;

    /// <summary>
    /// ADT: Opens the TTS voice selection window
    /// </summary>
    public void OpenADTTTSWindow()
    {
        if (Profile is null)
            return;

        if (_adtTtsWindow != null)
        {
            _adtTtsWindow.Close();
            _adtTtsWindow = null;
        }

        _adtTtsWindow = new DefaultWindow
        {
            Title = Loc.GetString("humanoid-profile-editor-tts-window-title")
        };

        var ttsTab = new TTSVoiceSelectionTab();
        ttsTab.SetProfileData(Profile.Voice, Profile.Sex);

        ttsTab.OnVoiceSelected += voiceId =>
        {
            SetVoice(voiceId);
            UpdateTTSVoicesControls();
        };

        _adtTtsWindow.Contents.AddChild(ttsTab);
        _adtTtsWindow.SetSize = new Vector2(640, 480);
        _adtTtsWindow.OpenCentered();

        _adtTtsWindow.OnClose += () =>
        {
            _adtTtsWindow = null;
        };
    }
}
