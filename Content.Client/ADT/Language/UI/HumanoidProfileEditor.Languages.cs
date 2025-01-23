using System.Linq;
using Content.Client.ADT.Language.UI;
using Content.Shared.ADT.Language;
using Content.Shared.Humanoid.Prototypes;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.Lobby.UI;

public sealed partial class HumanoidProfileEditor
{
    public void RefreshLanguages()
    {
        LanguagesList.DisposeAllChildren();
        TabContainer.SetTabTitle(1, Loc.GetString("humanoid-profile-editor-languages-tab"));
        SetDefaultLanguagesButton.OnPressed += SetDefaultLanguages;

        if (Profile == null)
            return;
        var species = _prototypeManager.Index(Profile.Species);

        LanguagesCountLabel.Text = Loc.GetString("humanoid-profile-editor-languages-count",
                                                ("current", Profile.Languages.Count),
                                                ("max", species.MaxLanguages));

        var list = _prototypeManager.EnumeratePrototypes<LanguagePrototype>().Where(x => x.Roundstart).ToList();
        foreach (var item in species.UniqueLanguages)
        {
            list.Add(_prototypeManager.Index(item));
        }

        list.Sort((x, y) => x.LocalizedName[0].CompareTo(y.LocalizedName[0]));
        list.Sort((x, y) => y.Priority.CompareTo(x.Priority));

        List<LanguagePrototype> defaultList = new();
        defaultList.AddRange(list.Where(x => species.DefaultLanguages.Contains(x) && !species.UniqueLanguages.Contains(x)));
        defaultList.AddRange(list.Where(x => species.UniqueLanguages.Contains(x)));
        defaultList.Sort((x, y) => x.LocalizedName[0].CompareTo(y.LocalizedName[0]));
        defaultList.Sort((x, y) => y.Priority.CompareTo(x.Priority));

        foreach (var item in defaultList)
        {
            AddLanguageEntry(item, species);
        }

        foreach (var item in list)
        {
            if (defaultList.Contains(item))
                continue;
            AddLanguageEntry(item, species);
        }
    }

    private void AddLanguageEntry(LanguagePrototype proto, SpeciesPrototype species)
    {
        if (Profile == null)
            return;
        var entry = new LanguageEntry(proto, false);
        entry.SelectButton.Text = Loc.GetString(!Profile.Languages.Contains(proto) ? "language-lobby-add-button" : "language-lobby-remove-button");
        entry.SelectButton.ToolTip = null;
        entry.SelectButton.Disabled = Profile.Languages.Count >= species.MaxLanguages && !Profile.Languages.Contains(proto);
        entry.OnLanguageSelected += SelectLanguage;
        entry.Margin = new(7);
        entry.MaxWidth = 750f;
        LanguagesList.AddChild(entry);
    }

    public void SelectLanguage(string protoId)
    {
        Profile = (Profile?.Languages.Contains(protoId) ?? false) ? Profile?.WithoutLanguage(protoId) : Profile?.WithLanguage(protoId);
        SetDirty();
        RefreshLanguages();
    }

    private void SetDefaultLanguages(BaseButton.ButtonEventArgs args)
    {
        if (Profile == null)
            return;
        var species = _prototypeManager.Index(Profile.Species);
        foreach (var item in Profile.Languages)
        {
            Profile = Profile?.WithoutLanguage(item);
        }
        foreach (var item in species.DefaultLanguages)
        {
            Profile = Profile?.WithLanguage(item);
        }

        SetDirty();
        RefreshLanguages();
    }
}
