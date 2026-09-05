using System.Globalization;
using System.Linq;
using Content.Client.ADT.UserInterface.Controls;
using Content.Shared.ADT.Sponsors;
using Content.Shared.ADT.TTS;
using Content.Shared.Clothing;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Preferences.Loadouts;
using Content.Shared.Roles;
using Content.Shared.Traits;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;

namespace Content.Client.ADT.Sponsors.UI;

public sealed class SponsorBenefitsEditor : BoxContainer
{
    private readonly IPrototypeManager _proto;
    private readonly LoadoutSystem _loadoutSystem;

    private readonly CheckBox _bypassJobs;
    private readonly CheckBox _bypassAntags;
    private readonly SponsorProtoPicker _excludedDepartments;
    private readonly SponsorProtoPicker _excludedJobs;

    private readonly CheckBox _allLoadouts;
    private readonly SponsorProtoPicker _loadouts;
    private readonly CheckBox _allMarkings;
    private readonly SponsorProtoPicker _markings;
    private readonly SponsorProtoPicker _species;
    private readonly SponsorProtoPicker _traits;
    private readonly CheckBox _allTtsVoices;
    private readonly SponsorProtoPicker _ttsVoices;

    private readonly CheckBox _hasOocColor;
    private readonly LegacyColorSelectorSliders _oocColor;
    private readonly CheckBox _customOocColor;
    private readonly SponsorColorList _ghostColors;
    private readonly CheckBox _customGhostColor;

    private readonly LineEdit _discordRoles;
    private readonly CheckBox _priorityJoin;
    private readonly LineEdit _extraSlots;

    public SponsorBenefitsEditor()
    {
        _proto = IoCManager.Resolve<IPrototypeManager>();
        _loadoutSystem = IoCManager.Resolve<IEntityManager>().System<LoadoutSystem>();

        Orientation = LayoutOrientation.Vertical;
        SeparationOverride = 2;

        AddHeader(Loc.GetString("adt-sponsor-editor-roles"));
        _bypassJobs = AddCheck(Loc.GetString("adt-sponsor-editor-bypass-jobs"));
        _bypassAntags = AddCheck(Loc.GetString("adt-sponsor-editor-bypass-antags"));
        _excludedDepartments = AddPicker(Loc.GetString("adt-sponsor-editor-excluded-departments"), BuildDepartments());
        _excludedJobs = AddPicker(Loc.GetString("adt-sponsor-editor-excluded-jobs"), BuildJobs());

        AddHeader(Loc.GetString("adt-sponsor-editor-customization"));
        _allLoadouts = AddCheck(Loc.GetString("adt-sponsor-editor-all-loadouts"));
        _loadouts = AddPicker(Loc.GetString("adt-sponsor-editor-loadouts"), BuildLoadouts());
        _allMarkings = AddCheck(Loc.GetString("adt-sponsor-editor-all-markings"));
        _markings = AddPicker(Loc.GetString("adt-sponsor-editor-markings"), BuildMarkings());
        _species = AddPicker(Loc.GetString("adt-sponsor-editor-species"), BuildSpecies());
        _traits = AddPicker(Loc.GetString("adt-sponsor-editor-traits"), BuildTraits());
        _allTtsVoices = AddCheck(Loc.GetString("adt-sponsor-editor-all-tts"));
        _ttsVoices = AddPicker(Loc.GetString("adt-sponsor-editor-tts"), BuildTtsVoices());

        AddHeader(Loc.GetString("adt-sponsor-editor-chat-ghost"));

        _hasOocColor = AddCheck(Loc.GetString("adt-sponsor-editor-has-ooc-color"));

        _oocColor = new LegacyColorSelectorSliders
        {
            Color = Color.White,
            Visible = false,
            Margin = new Thickness(12, 0, 0, 4),
        };

        _hasOocColor.OnToggled += args => _oocColor.Visible = args.Pressed;
        AddChild(_oocColor);

        _customOocColor = AddCheck(Loc.GetString("adt-sponsor-editor-custom-ooc"));

        _ghostColors = new SponsorColorList(Loc.GetString("adt-sponsor-editor-ghost-colors"));
        AddChild(_ghostColors);

        _customGhostColor = AddCheck(Loc.GetString("adt-sponsor-editor-custom-ghost"));

        AddHeader(Loc.GetString("adt-sponsor-editor-other"));

        _discordRoles = AddField(Loc.GetString("adt-sponsor-editor-discord-roles"), "1054908932868538449");

        _priorityJoin = AddCheck(Loc.GetString("adt-sponsor-editor-priority-join"));
        _extraSlots = AddField(Loc.GetString("adt-sponsor-editor-extra-slots"), "0");
    }

    public void Load(SponsorBenefits benefits)
    {
        _bypassJobs.Pressed = (benefits.RoleBypass & SponsorRoleBypass.Jobs) != 0;
        _bypassAntags.Pressed = (benefits.RoleBypass & SponsorRoleBypass.Antags) != 0;
        _excludedDepartments.SetSelected(benefits.ExcludedDepartments);
        _excludedJobs.SetSelected(benefits.ExcludedJobs);

        _allLoadouts.Pressed = benefits.AllLoadouts;
        _loadouts.SetSelected(benefits.Loadouts);
        _allMarkings.Pressed = benefits.AllMarkings;
        _markings.SetSelected(benefits.Markings);
        _species.SetSelected(benefits.Species);
        _traits.SetSelected(benefits.Traits);
        _allTtsVoices.Pressed = benefits.AllTtsVoices;
        _ttsVoices.SetSelected(benefits.TtsVoices);

        _hasOocColor.Pressed = benefits.OocColor != null;
        _oocColor.Visible = _hasOocColor.Pressed;
        _oocColor.Color = benefits.OocColor ?? Color.White;
        _customOocColor.Pressed = benefits.AllowCustomOocColor;
        _ghostColors.SetColors(benefits.GhostColors);
        _customGhostColor.Pressed = benefits.AllowCustomGhostColor;

        _discordRoles.Text = string.Join(", ", benefits.DiscordRoles);
        _priorityJoin.Pressed = benefits.PriorityJoin;
        _extraSlots.Text = benefits.ExtraCharacterSlots.ToString(CultureInfo.InvariantCulture);
    }

    public SponsorBenefits Build()
    {
        var benefits = new SponsorBenefits
        {
            ExcludedDepartments = _excludedDepartments.GetSelected(),
            ExcludedJobs = _excludedJobs.GetSelected(),
            Loadouts = _loadouts.GetSelected(),
            Markings = _markings.GetSelected(),
            Species = _species.GetSelected(),
            Traits = _traits.GetSelected(),
            TtsVoices = _ttsVoices.GetSelected(),
            AllTtsVoices = _allTtsVoices.Pressed,
            AllLoadouts = _allLoadouts.Pressed,
            AllMarkings = _allMarkings.Pressed,
            AllowCustomOocColor = _customOocColor.Pressed,
            AllowCustomGhostColor = _customGhostColor.Pressed,
            DiscordRoles = SplitRoles(_discordRoles.Text),
            PriorityJoin = _priorityJoin.Pressed,
            OocColor = _hasOocColor.Pressed ? _oocColor.Color : null,
            GhostColors = _ghostColors.GetColors(),
            ExtraCharacterSlots = ParseInt(_extraSlots.Text),
        };

        if (_bypassJobs.Pressed)
            benefits.RoleBypass |= SponsorRoleBypass.Jobs;

        if (_bypassAntags.Pressed)
            benefits.RoleBypass |= SponsorRoleBypass.Antags;

        return benefits;
    }

    public void Clear()
    {
        Load(new SponsorBenefits());
    }

    private IEnumerable<SponsorPickerItem> BuildDepartments()
    {
        foreach (var proto in _proto.EnumeratePrototypes<DepartmentPrototype>())
        {
            yield return new SponsorPickerItem(proto.ID, Loc.GetString(proto.Name));
        }
    }

    private IEnumerable<SponsorPickerItem> BuildJobs()
    {
        foreach (var proto in _proto.EnumeratePrototypes<JobPrototype>())
        {
            yield return new SponsorPickerItem(proto.ID, proto.LocalizedName);
        }
    }

    private IEnumerable<SponsorPickerItem> BuildLoadouts()
    {
        foreach (var proto in _proto.EnumeratePrototypes<LoadoutPrototype>())
        {
            yield return new SponsorPickerItem(proto.ID, _loadoutSystem.GetName(proto), proto.SponsorOnly);
        }
    }

    private IEnumerable<SponsorPickerItem> BuildMarkings()
    {
        foreach (var proto in _proto.EnumeratePrototypes<MarkingPrototype>())
        {
            yield return new SponsorPickerItem(proto.ID, Loc.GetString($"marking-{proto.ID}"), proto.SponsorOnly);
        }
    }

    private IEnumerable<SponsorPickerItem> BuildSpecies()
    {
        foreach (var proto in _proto.EnumeratePrototypes<SpeciesPrototype>())
        {
            yield return new SponsorPickerItem(proto.ID, Loc.GetString(proto.Name), proto.SponsorOnly);
        }
    }

    private IEnumerable<SponsorPickerItem> BuildTtsVoices()
    {
        foreach (var proto in _proto.EnumeratePrototypes<TTSVoicePrototype>())
        {
            yield return new SponsorPickerItem(proto.ID, proto.Name, proto.SponsorOnly);
        }
    }

    private IEnumerable<SponsorPickerItem> BuildTraits()
    {
        foreach (var proto in _proto.EnumeratePrototypes<TraitPrototype>())
        {
            yield return new SponsorPickerItem(proto.ID, Loc.GetString(proto.Name), proto.SponsorOnly);
        }
    }

    private void AddHeader(string text)
    {
        AddChild(new Label
        {
            Text = text,
            StyleClasses = { "LabelHeading" },
            Margin = new Thickness(0, 6, 0, 2),
        });
    }

    private CheckBox AddCheck(string text)
    {
        var check = new CheckBox
        {
            Text = text,
        };

        AddChild(check);
        return check;
    }

    private SponsorProtoPicker AddPicker(string title, IEnumerable<SponsorPickerItem> items)
    {
        var picker = new SponsorProtoPicker(title, items);
        AddChild(picker);
        return picker;
    }

    private LineEdit AddField(string label, string placeholder)
    {
        var row = new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
            SeparationOverride = 4,
        };

        row.AddChild(new Label
        {
            Text = label,
            MinWidth = 220,
        });

        var edit = new LineEdit
        {
            HorizontalExpand = true,
            PlaceHolder = placeholder,
        };

        row.AddChild(edit);
        AddChild(row);

        return edit;
    }

    private static HashSet<string> SplitRoles(string? raw)
    {
        var result = new HashSet<string>();

        if (string.IsNullOrWhiteSpace(raw))
            return result;

        foreach (var part in raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (ulong.TryParse(part, out _))
                result.Add(part);
        }

        return result;
    }

    private static int ParseInt(string? raw)
    {
        if (int.TryParse(raw, out var value) && value >= 0)
            return value;

        return 0;
    }
}
