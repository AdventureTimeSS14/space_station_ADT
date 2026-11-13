using System.Linq;
using Content.Client.ADT.Sponsors.UI;
using Content.Shared.ADT.Polls;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Players.PlayTimeTracking;
using Content.Shared.Roles;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.ADT.Polls.UI;

public sealed class PollEditor : BoxContainer
{
    private readonly IPrototypeManager _proto;

    private readonly Label _header;
    private readonly LineEdit _title;
    private readonly TextEdit _description;
    private readonly OptionButton _mode;
    private readonly LineEdit _hours;
    private readonly CheckBox _noEnd;
    private readonly Label _endsInfo;
    private readonly LineEdit _servers;
    private readonly Label _serverInfo;

    private readonly SponsorProtoPicker _jobs;
    private readonly SponsorProtoPicker _departments;
    private readonly SponsorProtoPicker _species;
    private readonly SponsorProtoPicker _antagRoles;
    private readonly OptionButton _antag;
    private readonly LineEdit _minHours;
    private readonly LineEdit _maxHours;
    private readonly OptionButton _tracker;
    private readonly LineEdit _trackerMin;
    private readonly LineEdit _trackerMax;
    private readonly CheckBox _requireRound;

    private readonly Label _lockedNote;
    private readonly BoxContainer _questions;
    private readonly Button _addQuestion;

    private readonly List<PollQuestionEditor> _questionEditors = new();
    private readonly List<string> _trackerIds = new();

    private int _id;
    private DateTime? _currentEndsAt;
    private bool _locked;

    public PollEditor(IPrototypeManager proto)
    {
        _proto = proto;

        Orientation = LayoutOrientation.Vertical;
        SeparationOverride = 4;
        Margin = new Thickness(6);

        _header = new Label
        {
            StyleClasses = { "LabelHeading" },
        };

        AddChild(_header);

        AddLabel(Loc.GetString("adt-poll-editor-title"));

        _title = new LineEdit
        {
            HorizontalExpand = true,
        };

        AddChild(_title);

        AddLabel(Loc.GetString("adt-poll-editor-description"));

        _description = new TextEdit
        {
            MinHeight = 80,
            HorizontalExpand = true,
        };

        AddChild(new PanelContainer
        {
            StyleClasses = { "BackgroundDark" },
            Children = { _description },
        });

        AddHeading(Loc.GetString("adt-poll-editor-show"));

        _mode = new OptionButton();

        var modes = new[]
        {
            PollShowMode.Immediately,
            PollShowMode.RoundStart,
            PollShowMode.RoundEnd,
        };

        foreach (var mode in modes)
        {
            _mode.AddItem(PollNames.ShowMode(mode), (int) mode);
        }

        _mode.OnItemSelected += args => _mode.SelectId(args.Id);
        AddRow(Loc.GetString("adt-poll-editor-mode"), _mode);

        _hours = new LineEdit
        {
            MinWidth = 80,
        };

        _noEnd = new CheckBox
        {
            Text = Loc.GetString("adt-poll-editor-no-end"),
        };

        _noEnd.OnToggled += _ => _hours.Editable = !_noEnd.Pressed;
        AddRow(Loc.GetString("adt-poll-editor-hours"), _hours, _noEnd);

        _endsInfo = new Label
        {
            FontColorOverride = Color.Gray,
        };

        AddChild(_endsInfo);

        _servers = new LineEdit
        {
            HorizontalExpand = true,
            PlaceHolder = Loc.GetString("adt-poll-editor-servers-placeholder"),
        };

        AddRow(Loc.GetString("adt-poll-editor-servers"), _servers);

        _serverInfo = new Label
        {
            FontColorOverride = Color.Gray,
        };

        AddChild(_serverInfo);

        AddHeading(Loc.GetString("adt-poll-editor-filters"));

        _requireRound = new CheckBox
        {
            Text = Loc.GetString("adt-poll-editor-require-round"),
        };

        AddChild(_requireRound);

        _jobs = AddPicker(Loc.GetString("adt-poll-editor-jobs"), BuildJobs());
        _departments = AddPicker(Loc.GetString("adt-poll-editor-departments"), BuildDepartments());
        _species = AddPicker(Loc.GetString("adt-poll-editor-species"), BuildSpecies());

        _antag = new OptionButton();
        _antag.AddItem(Loc.GetString("adt-poll-antag-any"), (int) PollAntagFilter.Any);
        _antag.AddItem(Loc.GetString("adt-poll-antag-only"), (int) PollAntagFilter.Only);
        _antag.AddItem(Loc.GetString("adt-poll-antag-exclude"), (int) PollAntagFilter.Exclude);
        _antag.OnItemSelected += args => _antag.SelectId(args.Id);
        AddRow(Loc.GetString("adt-poll-editor-antag"), _antag);

        _antagRoles = AddPicker(Loc.GetString("adt-poll-editor-antag-roles"), BuildAntags());

        _minHours = NumberEdit();
        _maxHours = NumberEdit();
        AddRow(Loc.GetString("adt-poll-editor-playtime"), _minHours, Dash(), _maxHours);

        _tracker = new OptionButton
        {
            MinWidth = 200,
        };

        _tracker.AddItem(Loc.GetString("adt-poll-editor-tracker-none"), 0);
        _trackerIds.Add(string.Empty);

        foreach (var tracker in _proto.EnumeratePrototypes<PlayTimeTrackerPrototype>().OrderBy(t => t.ID))
        {
            _tracker.AddItem(tracker.ID, _trackerIds.Count);
            _trackerIds.Add(tracker.ID);
        }

        _tracker.OnItemSelected += args => _tracker.SelectId(args.Id);

        _trackerMin = NumberEdit();
        _trackerMax = NumberEdit();
        AddRow(Loc.GetString("adt-poll-editor-tracker"), _tracker, _trackerMin, Dash(), _trackerMax);

        AddHeading(Loc.GetString("adt-poll-editor-questions"));

        _lockedNote = new Label
        {
            Text = Loc.GetString("adt-poll-editor-locked"),
            FontColorOverride = Color.Orange,
            Visible = false,
        };

        AddChild(_lockedNote);

        _questions = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            SeparationOverride = 6,
        };

        AddChild(_questions);

        _addQuestion = new Button
        {
            Text = Loc.GetString("adt-poll-editor-add-question"),
            HorizontalAlignment = HAlignment.Left,
        };

        _addQuestion.OnPressed += _ => AddQuestion(new PollQuestion());
        AddChild(_addQuestion);

        Load(null, 0, string.Empty);
    }

    public void Load(PollData? poll, int responses, string serverName)
    {
        poll ??= new PollData
        {
            Questions = new List<PollQuestion>
            {
                new(),
            },
        };

        _id = poll.Id;
        _currentEndsAt = poll.EndsAt;
        _locked = poll.Id != 0 && responses > 0;

        _header.Text = poll.Id == 0
            ? Loc.GetString("adt-poll-editor-new")
            : Loc.GetString("adt-poll-editor-edit", ("id", poll.Id));

        _title.Text = poll.Title;
        _description.TextRope = new Rope.Leaf(poll.Description);
        _mode.SelectId((int) poll.ShowMode);

        _noEnd.Pressed = poll.Id != 0 && poll.EndsAt == null;
        _hours.Editable = !_noEnd.Pressed;
        _hours.Text = poll.Id == 0 ? "72" : string.Empty;

        _endsInfo.Text = poll.EndsAt is { } endsAt
            ? Loc.GetString("adt-poll-editor-ends-current", ("date", endsAt.ToLocalTime().ToString("dd.MM.yyyy HH:mm")))
            : string.Empty;

        _servers.Text = string.Join(", ", poll.Servers);
        _serverInfo.Text = Loc.GetString("adt-poll-editor-server-current", ("server", serverName));

        var filters = poll.Filters;
        _requireRound.Pressed = filters.RequireRound;
        _jobs.SetSelected(filters.Jobs);
        _departments.SetSelected(filters.Departments);
        _species.SetSelected(filters.Species);
        _antagRoles.SetSelected(filters.AntagRoles);
        _antag.SelectId((int) filters.Antag);
        _minHours.Text = filters.MinPlaytimeHours?.ToString() ?? string.Empty;
        _maxHours.Text = filters.MaxPlaytimeHours?.ToString() ?? string.Empty;

        var trackerIndex = filters.Tracker == null ? 0 : Math.Max(0, _trackerIds.IndexOf(filters.Tracker));
        _tracker.SelectId(trackerIndex);
        _trackerMin.Text = filters.TrackerMinHours?.ToString() ?? string.Empty;
        _trackerMax.Text = filters.TrackerMaxHours?.ToString() ?? string.Empty;

        _questions.RemoveAllChildren();
        _questionEditors.Clear();

        foreach (var question in poll.Questions)
        {
            AddQuestion(question);
        }

        _lockedNote.Visible = _locked;
        _addQuestion.Disabled = _locked;
        RefreshQuestions();
    }

    public PollData Build()
    {
        var poll = new PollData
        {
            Id = _id,
            Title = _title.Text.Trim(),
            Description = Rope.Collapse(_description.TextRope).Trim(),
            ShowMode = (PollShowMode) _mode.SelectedId,
            Questions = _questionEditors.Select(e => e.Build()).ToList(),
        };

        if (_noEnd.Pressed)
            poll.EndsAt = null;
        else if (double.TryParse(_hours.Text, out var hours) && hours > 0)
            poll.EndsAt = DateTime.UtcNow + TimeSpan.FromHours(hours);
        else
            poll.EndsAt = _currentEndsAt;

        foreach (var server in _servers.Text.Split(','))
        {
            var trimmed = server.Trim();

            if (trimmed.Length > 0)
                poll.Servers.Add(trimmed);
        }

        poll.Filters = new PollFilters
        {
            RequireRound = _requireRound.Pressed,
            Jobs = _jobs.GetSelected(),
            Departments = _departments.GetSelected(),
            Species = _species.GetSelected(),
            AntagRoles = _antagRoles.GetSelected(),
            Antag = (PollAntagFilter) _antag.SelectedId,
            MinPlaytimeHours = ParseHours(_minHours),
            MaxPlaytimeHours = ParseHours(_maxHours),
        };

        if (_tracker.SelectedId > 0 && _tracker.SelectedId < _trackerIds.Count)
        {
            poll.Filters.Tracker = _trackerIds[_tracker.SelectedId];
            poll.Filters.TrackerMinHours = ParseHours(_trackerMin);
            poll.Filters.TrackerMaxHours = ParseHours(_trackerMax);
        }

        return poll;
    }

    private void AddQuestion(PollQuestion question)
    {
        var editor = new PollQuestionEditor(question);

        editor.MoveRequested += MoveQuestion;
        editor.RemoveRequested += RemoveQuestion;

        _questionEditors.Add(editor);
        _questions.AddChild(editor);
        RefreshQuestions();
    }

    private void MoveQuestion(PollQuestionEditor editor, int delta)
    {
        var index = _questionEditors.IndexOf(editor);
        var target = index + delta;

        if (index < 0 || target < 0 || target >= _questionEditors.Count)
            return;

        _questionEditors.RemoveAt(index);
        _questionEditors.Insert(target, editor);
        editor.SetPositionInParent(target);
        RefreshQuestions();
    }

    private void RemoveQuestion(PollQuestionEditor editor)
    {
        _questionEditors.Remove(editor);
        _questions.RemoveChild(editor);
        RefreshQuestions();
    }

    private void RefreshQuestions()
    {
        for (var i = 0; i < _questionEditors.Count; i++)
        {
            _questionEditors[i].SetIndex(i, _questionEditors.Count);
            _questionEditors[i].SetLocked(_locked);
        }
    }

    private static int? ParseHours(LineEdit edit)
    {
        if (int.TryParse(edit.Text.Trim(), out var value) && value >= 0)
            return value;

        return null;
    }

    private static Label Dash()
    {
        return new Label
        {
            Text = "-",
        };
    }

    private static LineEdit NumberEdit()
    {
        return new LineEdit
        {
            MinWidth = 60,
        };
    }

    private void AddLabel(string text)
    {
        AddChild(new Label
        {
            Text = text,
            FontColorOverride = Color.Gray,
        });
    }

    private void AddHeading(string text)
    {
        AddChild(new Label
        {
            Text = text,
            StyleClasses = { "LabelHeading" },
            Margin = new Thickness(0, 8, 0, 2),
        });
    }

    private void AddRow(string label, params Control[] controls)
    {
        var row = new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
            SeparationOverride = 4,
        };

        row.AddChild(new Label
        {
            Text = label,
            MinWidth = 200,
        });

        foreach (var control in controls)
        {
            row.AddChild(control);
        }

        AddChild(row);
    }

    private SponsorProtoPicker AddPicker(string title, IEnumerable<SponsorPickerItem> items)
    {
        var picker = new SponsorProtoPicker(title, items);
        AddChild(picker);
        return picker;
    }

    private IEnumerable<SponsorPickerItem> BuildJobs()
    {
        foreach (var proto in _proto.EnumeratePrototypes<JobPrototype>())
        {
            yield return new SponsorPickerItem(proto.ID, proto.LocalizedName);
        }
    }

    private IEnumerable<SponsorPickerItem> BuildDepartments()
    {
        foreach (var proto in _proto.EnumeratePrototypes<DepartmentPrototype>())
        {
            yield return new SponsorPickerItem(proto.ID, Loc.GetString(proto.Name));
        }
    }

    private IEnumerable<SponsorPickerItem> BuildSpecies()
    {
        foreach (var proto in _proto.EnumeratePrototypes<SpeciesPrototype>())
        {
            yield return new SponsorPickerItem(proto.ID, Loc.GetString(proto.Name));
        }
    }

    private IEnumerable<SponsorPickerItem> BuildAntags()
    {
        foreach (var proto in _proto.EnumeratePrototypes<AntagPrototype>())
        {
            yield return new SponsorPickerItem(proto.ID, Loc.GetString(proto.Name));
        }
    }
}
