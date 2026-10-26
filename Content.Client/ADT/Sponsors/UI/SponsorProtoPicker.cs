using System.Linq;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.ADT.Sponsors.UI;

public readonly record struct SponsorPickerItem(string Id, string Name, bool Primary = true);

public sealed class SponsorProtoPicker : BoxContainer
{
    private readonly string _title;
    private readonly Button _header;
    private readonly BoxContainer _body;
    private readonly LineEdit _search;
    private readonly CheckBox _showAll;
    private readonly BoxContainer _list;

    private readonly Dictionary<string, Entry> _entries = new();

    private sealed class Entry
    {
        public required CheckBox Box;
        public required string SearchText;
        public required bool Primary;
    }

    public SponsorProtoPicker(string title, IEnumerable<SponsorPickerItem> items)
    {
        _title = title;

        Orientation = LayoutOrientation.Vertical;
        SeparationOverride = 2;

        _header = new Button
        {
            ToggleMode = true,
            HorizontalAlignment = HAlignment.Stretch,
        };

        AddChild(_header);

        _search = new LineEdit
        {
            PlaceHolder = Loc.GetString("adt-sponsor-picker-search"),
        };

        _showAll = new CheckBox
        {
            Text = Loc.GetString("adt-sponsor-picker-show-all"),
            Visible = false,
        };

        _list = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            SeparationOverride = 0,
        };

        _body = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            SeparationOverride = 2,
            Visible = false,
            Margin = new Thickness(12, 0, 0, 4),
        };

        _body.AddChild(_search);
        _body.AddChild(_showAll);
        _body.AddChild(new ScrollContainer
        {
            MinHeight = 180,
            HScrollEnabled = false,
            Children = { _list },
        });

        AddChild(_body);

        foreach (var item in items.OrderBy(i => i.Name, StringComparer.CurrentCultureIgnoreCase))
        {
            AddEntry(item);
        }

        _showAll.Visible = _entries.Values.Any(e => !e.Primary);

        _header.OnToggled += args => _body.Visible = args.Pressed;
        _search.OnTextChanged += _ => ApplyFilter();
        _showAll.OnToggled += _ => ApplyFilter();

        UpdateHeader();
        ApplyFilter();
    }

    public void SetSelected(IEnumerable<string> selected)
    {
        foreach (var entry in _entries.Values)
        {
            entry.Box.Pressed = false;
        }

        foreach (var id in selected)
        {
            if (!_entries.TryGetValue(id, out var entry))
            {
                entry = AddEntry(new SponsorPickerItem(id, Loc.GetString("adt-sponsor-picker-missing", ("id", id))));
                _entries[id] = entry;
            }

            entry.Box.Pressed = true;
        }

        UpdateHeader();
        ApplyFilter();
    }

    public HashSet<string> GetSelected()
    {
        var result = new HashSet<string>();

        foreach (var (id, entry) in _entries)
        {
            if (entry.Box.Pressed)
                result.Add(id);
        }

        return result;
    }

    private Entry AddEntry(SponsorPickerItem item)
    {
        var box = new CheckBox
        {
            Text = item.Name == item.Id ? item.Id : $"{item.Name}  [{item.Id}]",
        };

        var entry = new Entry
        {
            Box = box,
            SearchText = $"{item.Name} {item.Id}",
            Primary = item.Primary,
        };

        box.OnToggled += _ => UpdateHeader();

        _entries[item.Id] = entry;
        _list.AddChild(box);

        return entry;
    }

    private void ApplyFilter()
    {
        var query = _search.Text.Trim();
        var showAll = _showAll.Pressed;

        foreach (var entry in _entries.Values)
        {
            if (entry.Box.Pressed)
            {
                entry.Box.Visible = true;
                continue;
            }

            if (!entry.Primary && !showAll)
            {
                entry.Box.Visible = false;
                continue;
            }

            entry.Box.Visible = query.Length == 0
                                || entry.SearchText.Contains(query, StringComparison.CurrentCultureIgnoreCase);
        }
    }

    private void UpdateHeader()
    {
        var count = _entries.Values.Count(e => e.Box.Pressed);
        _header.Text = count == 0 ? _title : $"{_title}  ({count})";
    }
}
