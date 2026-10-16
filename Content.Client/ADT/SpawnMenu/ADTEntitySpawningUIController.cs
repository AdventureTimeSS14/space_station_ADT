using Content.Client.ADT.SpawnMenu.UI;
using Content.Shared.ADT.SpawnMenu;
using JetBrains.Annotations;
using Robust.Client.Placement;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controllers.Implementations;
using Robust.Client.UserInterface.Controls;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using static Robust.Client.UserInterface.Controls.BaseButton;

namespace Content.Client.ADT.SpawnMenu;

[UsedImplicitly]
public sealed class ADTEntitySpawningUIController : UIController
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IPlacementManager _placement = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;

    private ADTEntitySpawnWindow? _window;

    private ADTEntitySpawnIndex? _index;
    private readonly List<ADTSpawnListItem> _shown = new();
    private readonly List<(ADTEntitySpawnEntry Entry, int Rank, int Order)> _matched = new();
    private readonly List<(string Raw, string Normalized)> _activeTags = new();
    private ADTSpawnSearchQuery _query = ADTSpawnSearchQuery.Parse(string.Empty);
    private bool _indexDirty = true;
    private string _selectedCategory = string.Empty;
    private bool _showService;
    private bool _groupVariants = true;
    private ADTSpawnIdList _favorites = default!;
    private ADTSpawnIdList _recent = default!;

    private const int RecentLimit = 30;

    private ADTSpawnMenuStorageUIController Storage => UIManager.GetUIController<ADTSpawnMenuStorageUIController>();
    private ADTSpawnWindowData Settings => Storage.Data.Entities;

    public override void Initialize()
    {
        base.Initialize();

        _favorites = new ADTSpawnIdList(Settings.Favorites, Storage.QueueSave);
        _recent = new ADTSpawnIdList(Settings.Recent, Storage.QueueSave, RecentLimit);
        _showService = Settings.ShowService;
        _groupVariants = Settings.GroupVariants;
        _selectedCategory = Settings.Category;

        _placement.DirectionChanged += OnDirectionChanged;
        _placement.PlacementChanged += OnPlacementChanged;
        _prototypes.PrototypesReloaded += OnPrototypesReloaded;
        _cfg.OnValueChanged(CVars.EntitiesCategoryFilter, _ => OnCategoryFilterChanged());
    }

    public void ToggleWindow()
    {
        EnsureWindow();

        if (_window!.IsOpen)
        {
            _window.Close();
            return;
        }

        if (_indexDirty)
            RebuildIndex();

        ADTSpawnIdList.OpenToLeft(_window, UIManager.WindowRoot.Size);
        UpdateDirectionLabel();
    }

    public void CloseWindow()
    {
        if (_window == null || _window.Disposed)
            return;

        _window.Close();
    }

    private void EnsureWindow()
    {
        if (_window is { Disposed: false })
            return;

        _window = UIManager.CreateWindow<ADTEntitySpawnWindow>();

        _window.OnClose += OnWindowClosed;
        _window.OnPrototypeSelected += OnPrototypeSelected;
        _window.OnSearchChanged += OnSearchChanged;
        _window.OnCategorySelected += OnCategorySelected;
        _window.ShowServiceCheckBox.Pressed = _showService;
        _window.ShowServiceCheckBox.OnToggled += OnShowServiceToggled;
        _window.GroupCheckBox.Pressed = _groupVariants;
        _window.GroupCheckBox.OnToggled += OnGroupToggled;
        _window.OnTagToggled += OnTagToggled;
        _window.ReplaceButton.Pressed = _placement.Replacement;
        _window.ReplaceButton.OnToggled += OnReplaceToggled;
        _window.EraseButton.Pressed = _placement.Eraser;
        _window.EraseButton.OnToggled += OnEraseToggled;
        _window.OverrideMenu.OnItemSelected += OnOverrideSelected;
        _window.OldWindowButton.OnPressed += OnOldWindowPressed;
        _window.OnFavoriteToggled += OnFavoriteToggled;
        _window.SetFavoriteChecker(_favorites.Contains);
        _window.SetGridMode(Settings.Grid);
        _window.SetDetailsVisible(Settings.Details);
        ADTSpawnIdList.LoadSize(Settings.Size, _window);

        _query = ADTSpawnSearchQuery.Parse(string.Empty);
        _activeTags.Clear();
        _indexDirty = true;
    }

    private void RebuildIndex()
    {
        if (_window == null || _window.Disposed)
            return;

        _indexDirty = false;
        _index ??= new ADTEntitySpawnIndex();
        _index.Rebuild();

        _window.SetCategories(_index.Roots, GetSelectedNode());
        UpdateShown();
    }

    private ADTSpawnCategoryNode GetSelectedNode()
    {
        return _index!.GetNode(_selectedCategory) ?? _index.All;
    }

    private void UpdateShown()
    {
        if (_window == null || _window.Disposed || _index == null)
            return;

        _prototypes.TryIndex<EntityCategoryPrototype>(_cfg.GetCVar(CVars.EntitiesCategoryFilter), out var filter);

        foreach (var node in _index.Nodes)
        {
            node.Count = 0;
        }

        var selected = GetSelectedNode();
        _matched.Clear();

        for (var i = 0; i < _index.Entries.Count; i++)
        {
            var entry = _index.Entries[i];
            if (filter != null && !entry.Prototype.Categories.Contains(filter))
                continue;

            if (!MatchesActiveTags(entry) || !_query.Matches(entry, _index))
                continue;

            ADTSpawnCategoryNode? normal = null;
            if (entry.ServiceCategory == null || _showService)
                normal = entry.Category ?? _index.Other;

            var favorite = _favorites.Contains(entry.Prototype.ID);
            var recent = _recent.Contains(entry.Prototype.ID);

            AddCount(normal);
            AddCount(entry.ServiceCategory);
            _index.All.Count++;

            if (favorite)
                _index.Favorites.Count++;

            if (recent)
                _index.Recent.Count++;

            if (entry.Untranslated && _index.Untranslated != null)
                _index.Untranslated.Count++;

            if (!IsInSelected(selected, entry, normal, favorite, recent))
                continue;

            _matched.Add((entry, _query.Rank(entry), i));
        }

        if (selected == _index.Recent && _query.RankToken == null)
        {
            _matched.Sort((a, b) =>
                _recent.IndexOf(a.Entry.Prototype.ID).CompareTo(_recent.IndexOf(b.Entry.Prototype.ID)));
        }
        else if (_query.RankToken != null)
        {
            _matched.Sort((a, b) =>
            {
                var rank = a.Rank.CompareTo(b.Rank);
                if (rank != 0)
                    return rank;

                return a.Order.CompareTo(b.Order);
            });
        }

        BuildItems();

        _window.UpdateCategoryCounts(!_query.IsEmpty || _activeTags.Count > 0);
        _window.SetTagBar(_activeTags);
        _window.SetItems(_shown, _matched.Count);
    }

    private bool IsInSelected(ADTSpawnCategoryNode selected,
        ADTEntitySpawnEntry entry,
        ADTSpawnCategoryNode? normal,
        bool favorite,
        bool recent)
    {
        if (selected == _index!.All)
            return true;

        if (selected == _index.Favorites)
            return favorite;

        if (selected == _index.Recent)
            return recent;

        if (selected == _index.Untranslated)
            return entry.Untranslated;

        return selected.Contains(normal) || selected.Contains(entry.ServiceCategory);
    }

    private void OnFavoriteToggled(EntityPrototype prototype)
    {
        _favorites.Toggle(prototype.ID);
        UpdateShown();
    }

    private bool MatchesActiveTags(ADTEntitySpawnEntry entry)
    {
        foreach (var (_, normalized) in _activeTags)
        {
            if (!entry.HasTag(normalized))
                return false;
        }

        return true;
    }

    private void BuildItems()
    {
        _shown.Clear();

        if (!_groupVariants)
        {
            foreach (var (entry, _, _) in _matched)
            {
                _shown.Add(new ADTSpawnListItem(entry, new List<ADTEntitySpawnEntry> { entry }));
            }

            return;
        }

        var groups = new Dictionary<(string, ADTSpawnCategoryNode?, ADTSpawnCategoryNode?), List<ADTEntitySpawnEntry>>();
        var order = new List<List<ADTEntitySpawnEntry>>();

        foreach (var (entry, _, _) in _matched)
        {
            var key = (entry.NameLower, entry.Category, entry.ServiceCategory);
            if (!groups.TryGetValue(key, out var variants))
            {
                variants = new List<ADTEntitySpawnEntry>();
                groups.Add(key, variants);
                order.Add(variants);
            }

            variants.Add(entry);
        }

        foreach (var variants in order)
        {
            var representative = variants[0];
            foreach (var variant in variants)
            {
                if (variant.SuffixTags.Count != 0)
                    continue;

                representative = variant;
                break;
            }

            _shown.Add(new ADTSpawnListItem(representative, variants));
        }
    }

    private void OnTagToggled(string raw, string normalized, bool add)
    {
        var index = _activeTags.FindIndex(t => t.Normalized == normalized);
        if (add && index < 0)
            _activeTags.Add((raw, normalized));
        else if (!add && index >= 0)
            _activeTags.RemoveAt(index);
        else
            return;

        UpdateShown();
    }

    private void OnGroupToggled(ButtonToggledEventArgs args)
    {
        _groupVariants = args.Pressed;
        UpdateShown();
    }

    private static void AddCount(ADTSpawnCategoryNode? node)
    {
        while (node != null)
        {
            node.Count++;
            node = node.Parent;
        }
    }

    private void OnCategorySelected(ADTSpawnCategoryNode? node)
    {
        _selectedCategory = node?.Id ?? string.Empty;
        UpdateShown();
    }

    private void OnShowServiceToggled(ButtonToggledEventArgs args)
    {
        _showService = args.Pressed;
        UpdateShown();
    }

    private void OnSearchChanged(string text)
    {
        _query = ADTSpawnSearchQuery.Parse(text);
        UpdateShown();
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (!args.WasModified<EntityPrototype>() && !args.WasModified<ADTSpawnCategoryPrototype>())
            return;

        _indexDirty = true;
        _index?.InvalidatePaths();

        if (_window is { Disposed: false, IsOpen: true })
            RebuildIndex();
    }

    private void OnCategoryFilterChanged()
    {
        if (_window is { Disposed: false, IsOpen: true })
            UpdateShown();
    }

    private void OnPrototypeSelected(EntityPrototype? prototype)
    {
        if (_window == null || _window.Disposed)
            return;

        if (prototype == null)
        {
            _window.SetSelected(null);
            _placement.Clear();
            return;
        }

        var overrideMode = _placement.AllModeNames[_window.OverrideMenu.SelectedId];
        var info = new PlacementInformation
        {
            PlacementOption = overrideMode != IPlacementManager.DefaultModeName ? overrideMode : prototype.PlacementMode,
            EntityType = prototype.ID,
            Range = 2,
            IsTile = false,
        };

        _placement.BeginPlacing(info);
        _recent.Push(prototype.ID);
        _window.SetSelected(prototype);
    }

    private void OnReplaceToggled(ButtonToggledEventArgs args)
    {
        _placement.Replacement = args.Pressed;
        args.Button.Pressed = args.Pressed;
    }

    private void OnEraseToggled(ButtonToggledEventArgs args)
    {
        if (_window == null || _window.Disposed)
            return;

        _placement.Clear();

        if (args.Pressed)
            _placement.ToggleEraser();

        args.Button.Pressed = args.Pressed;
        _window.OverrideMenu.Disabled = args.Pressed;
    }

    private void OnOverrideSelected(OptionButton.ItemSelectedEventArgs args)
    {
        if (_window == null || _window.Disposed)
            return;

        _window.OverrideMenu.SelectId(args.Id);

        if (_placement.CurrentMode == null || _placement.CurrentPermission == null)
            return;

        var info = new PlacementInformation
        {
            PlacementOption = _placement.AllModeNames[args.Id],
            EntityType = _placement.CurrentPermission.EntityType,
            Range = 2,
            IsTile = _placement.CurrentPermission.IsTile,
        };

        _placement.Clear();
        _placement.BeginPlacing(info);
    }

    private void OnOldWindowPressed(ButtonEventArgs args)
    {
        CloseWindow();
        UIManager.GetUIController<EntitySpawningUIController>().ToggleWindow();
    }

    private void OnWindowClosed()
    {
        if (_window == null || _window.Disposed)
            return;

        _window.SetSelected(null);
        _placement.Clear();

        Settings.Grid = _window.IsGrid;
        Settings.Details = _window.DetailsButton.Pressed;
        Settings.GroupVariants = _groupVariants;
        Settings.ShowService = _showService;
        Settings.Category = _selectedCategory;
        Settings.Size = _window.Size;
        Storage.QueueSave();
    }

    private void OnPlacementChanged(object? sender, EventArgs e)
    {
        if (_window == null || _window.Disposed)
            return;

        _window.SetSelected(null);
        _window.EraseButton.Pressed = false;
        _window.OverrideMenu.Disabled = false;
    }

    private void OnDirectionChanged(object? sender, EventArgs e)
    {
        UpdateDirectionLabel();
    }

    private void UpdateDirectionLabel()
    {
        if (_window == null || _window.Disposed)
            return;

        _window.RotationLabel.Text = Loc.GetString("adt-spawn-menu-rotation", ("direction", _placement.Direction.ToString()));
    }
}
