using Content.Client.ADT.SpawnMenu.UI;
using Content.Client.Gameplay;
using Content.Client.UserInterface.Systems.DecalPlacer;
using Content.Shared.Decals;
using JetBrains.Annotations;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;
using static Robust.Client.UserInterface.Controls.BaseButton;

namespace Content.Client.ADT.SpawnMenu;

[UsedImplicitly]
public sealed class ADTDecalSpawningUIController : UIController, IOnStateExited<GameplayState>
{
    [Dependency] private IPrototypeManager _prototypes = default!;

    private const int RecentLimit = 30;

    private ADTDecalSpawnWindow? _window;
    private ADTDecalSpawnIndex? _index;
    private ADTSpawnIdList _favorites = default!;
    private ADTSpawnIdList _recent = default!;

    private readonly List<ADTDecalSpawnEntry> _matched = new();
    private readonly List<ADTDecalSpawnItem> _shown = new();
    private string _search = string.Empty;
    private string _selectedCategory = string.Empty;
    private bool _groupDirections = true;
    private bool _indexDirty = true;

    private ADTSpawnMenuStorageUIController Storage => UIManager.GetUIController<ADTSpawnMenuStorageUIController>();
    private ADTSpawnWindowData Settings => Storage.Data.Decals;

    public override void Initialize()
    {
        base.Initialize();

        _favorites = new ADTSpawnIdList(Settings.Favorites, Storage.QueueSave);
        _recent = new ADTSpawnIdList(Settings.Recent, Storage.QueueSave, RecentLimit);
        _groupDirections = Settings.GroupVariants;

        _prototypes.PrototypesReloaded += OnPrototypesReloaded;
    }

    public void OnStateExited(GameplayState state)
    {
        if (_window == null)
            return;

        _window.Close();
        _window.Orphan();
        _window = null;
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

        _window = UIManager.CreateWindow<ADTDecalSpawnWindow>();

        _window.IsFavorite = _favorites.Contains;
        _window.OnClose += OnWindowClosed;
        _window.OnSearchChanged += OnSearchChanged;
        _window.OnCategorySelected += OnCategorySelected;
        _window.OnDecalSelected += OnDecalSelected;
        _window.OnFavoriteToggled += OnFavoriteToggled;
        _window.OnRecentColorsChanged += SaveRecentColors;
        _window.GroupCheckBox.Pressed = _groupDirections;
        _window.GroupCheckBox.OnToggled += OnGroupToggled;
        _window.OldWindowButton.OnPressed += OnOldWindowPressed;
        _window.SetGridMode(Settings.Grid);
        _window.SetRecentColors(Settings.RecentColors);
        ADTSpawnIdList.LoadSize(Settings.Size, _window);

        _search = string.Empty;
        _indexDirty = true;
    }

    private void RebuildIndex()
    {
        if (_window == null || _window.Disposed)
            return;

        _indexDirty = false;
        _index ??= new ADTDecalSpawnIndex();
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

        foreach (var node in _index.Nodes)
        {
            node.Count = 0;
        }

        var selected = GetSelectedNode();
        _matched.Clear();

        foreach (var entry in _index.Entries)
        {
            if (_search.Length > 0 && !entry.SearchKey.Contains(_search, StringComparison.Ordinal))
                continue;

            var favorite = _favorites.Contains(entry.Id);
            var recent = _recent.Contains(entry.Id);

            AddCount(entry.Category);
            _index.All.Count++;

            if (favorite)
                _index.Favorites.Count++;

            if (recent)
                _index.Recent.Count++;

            if (selected == _index.All
                || (selected == _index.Favorites && favorite)
                || (selected == _index.Recent && recent)
                || selected.Contains(entry.Category))
            {
                _matched.Add(entry);
            }
        }

        if (selected == _index.Recent)
            _matched.Sort((a, b) => _recent.IndexOf(a.Id).CompareTo(_recent.IndexOf(b.Id)));

        BuildItems();

        _window.UpdateCategoryCounts(_search.Length > 0);
        _window.SetItems(_shown, _matched.Count);
    }

    private void BuildItems()
    {
        _shown.Clear();

        if (!_groupDirections)
        {
            foreach (var entry in _matched)
            {
                _shown.Add(new ADTDecalSpawnItem(entry, new List<ADTDecalSpawnEntry> { entry }, entry.Id));
            }

            return;
        }

        var groups = new Dictionary<string, List<ADTDecalSpawnEntry>>();
        var order = new List<(string Key, List<ADTDecalSpawnEntry> Variants)>();

        foreach (var entry in _matched)
        {
            if (!groups.TryGetValue(entry.GroupKey, out var variants))
            {
                variants = new List<ADTDecalSpawnEntry>();
                groups.Add(entry.GroupKey, variants);
                order.Add((entry.GroupKey, variants));
            }

            variants.Add(entry);
        }

        foreach (var (key, variants) in order)
        {
            var name = variants.Count > 1 ? key : variants[0].Id;
            _shown.Add(new ADTDecalSpawnItem(variants[0], variants, name));
        }
    }

    private static void AddCount(ADTSpawnCategoryNode? node)
    {
        while (node != null)
        {
            node.Count++;
            node = node.Parent;
        }
    }

    private void OnSearchChanged(string text)
    {
        _search = text.Trim().ToLowerInvariant();
        UpdateShown();
    }

    private void OnCategorySelected(ADTSpawnCategoryNode? node)
    {
        _selectedCategory = node?.Id ?? string.Empty;
        UpdateShown();
    }

    private void OnGroupToggled(ButtonToggledEventArgs args)
    {
        _groupDirections = args.Pressed;
        UpdateShown();
    }

    private void OnDecalSelected(ADTDecalSpawnEntry entry)
    {
        if (_window == null || _window.Disposed)
            return;

        _recent.Push(entry.Id);
        _window.Select(entry);
    }

    private void OnFavoriteToggled(ADTDecalSpawnEntry entry)
    {
        _favorites.Toggle(entry.Id);
        UpdateShown();
        _window?.RefreshDetails();
    }

    private void OnOldWindowPressed(ButtonEventArgs args)
    {
        CloseWindow();
        UIManager.GetUIController<DecalPlacerUIController>().ToggleWindow();
    }

    private void OnWindowClosed()
    {
        if (_window == null || _window.Disposed)
            return;

        Settings.Grid = _window.IsGrid;
        Settings.GroupVariants = _groupDirections;
        Settings.Size = _window.Size;
        SaveRecentColors();
    }

    private void SaveRecentColors()
    {
        if (_window == null)
            return;

        Settings.RecentColors.Clear();
        Settings.RecentColors.AddRange(_window.RecentColorList);
        Storage.QueueSave();
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (!args.WasModified<DecalPrototype>())
            return;

        _indexDirty = true;

        if (_window is { Disposed: false, IsOpen: true })
            RebuildIndex();
    }
}
