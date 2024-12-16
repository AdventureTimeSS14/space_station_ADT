using System.Linq;
using Content.Client.ADT.SpeechBarks;
using Content.Shared.ADT.SpeechBarks;

namespace Content.Client.Lobby.UI;

public sealed partial class HumanoidProfileEditor
{
    private List<BarkPrototype> _barkList = new();

    private void InitializeBarks()  // У меня случился небольшой затуп. Оно барки в обход кнопки сохраняет, но хотя бы работает                                                                                                                                                             ы
    {
        _barkList = _prototypeManager
            .EnumeratePrototypes<BarkPrototype>()
            .Where(o => o.RoundStart)
            .OrderBy(o => Loc.GetString(o.Name))
            .ToList();

        BarkProtoButton.OnItemSelected += args =>
        {
            BarkProtoButton.SelectId(args.Id);
            SetBarkProto(_barkList[args.Id].ID);
            UpdateSaveButton();
        };

        PitchEdit.OnTextChanged += args =>
        {
            if (!float.TryParse(args.Text, out var newPitch))
                return;

            SetBarkPitch(newPitch);
            UpdateSaveButton();
        };

        DelayVariationMinEdit.OnTextChanged += args =>
        {
            if (!float.TryParse(args.Text, out var newVar))
                return;

            SetBarkMinVariation(newVar);
            UpdateSaveButton();
        };

        DelayVariationMaxEdit.OnTextChanged += args =>
        {
            if (!float.TryParse(args.Text, out var newVar))
                return;

            SetBarkMaxVariation(newVar);
            UpdateSaveButton();
        };

        BarkPlayButton.OnPressed += _ => PlayPreviewBark();
    }

    private void UpdateBarkVoicesControls()
    {
        if (Profile is null)
            return;

        BarkProtoButton.Clear();

        PitchEdit.Text = Profile.Bark.Pitch.ToString();
        DelayVariationMinEdit.Text = Profile.Bark.MinVar.ToString();
        DelayVariationMaxEdit.Text = Profile.Bark.MaxVar.ToString();

        var firstVoiceChoiceId = 1;
        for (var i = 0; i < _barkList.Count; i++)
        {
            var voice = _barkList[i];

            var name = Loc.GetString(voice.Name);
            BarkProtoButton.AddItem(name, i);

            if (firstVoiceChoiceId == 1)
                firstVoiceChoiceId = i;
        }

        var voiceChoiceId = _barkList.FindIndex(x => x.ID == Profile.Bark.Proto);
        if (!BarkProtoButton.TrySelectId(voiceChoiceId) &&
            BarkProtoButton.TrySelectId(firstVoiceChoiceId))
        {
            SetBarkProto(_barkList[firstVoiceChoiceId].ID);
        }
    }

    private void PlayPreviewBark()
    {
        if (Profile is null)
            return;

        _entManager.System<SpeechBarksSystem>().PlayDataPrewiew(Profile.Bark.Proto, Profile.Bark.Pitch, Profile.Bark.MinVar, Profile.Bark.MaxVar);
    }
}
