using Content.Client.ADT.SpawnMenu.UI;
using Content.Shared.Maps;
using JetBrains.Annotations;
using Robust.Client.Placement;
using Robust.Client.Placement.Modes;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controllers.Implementations;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using static Robust.Client.UserInterface.Controls.BaseButton;

namespace Content.Client.ADT.SpawnMenu;

[UsedImplicitly]
public sealed class ADTTileSpawningUIController : UIController
{
    [Dependency] private IPlacementManager _placement = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;
    [Dependency] private ITileDefinitionManager _tiles = default!;

    private const int RecentLimit = 20;

    private ADTTileSpawnWindow? _window;
    private ADTTileSpawnIndex? _index;
    private ADTSpawnIdList _favorites = default!;
    private ADTSpawnIdList _recent = default!;

    private readonly List<ADTTileSpawnEntry> _shown = new();
    private string _search = string.Empty;
    private string _selectedCategory = string.Empty;
    private bool _indexDirty = true;
    private bool _eraseTile;

    private ADTSpawnMenuStorageUIController Storage => UIManager.GetUIController<ADTSpawnMenuStorageUIController>();
    private ADTSpawnWindowData Settings => Storage.Data.Tiles;

    public override void Initialize()
    {
        base.Initialize();

        _favorites = new ADTSpawnIdList(Settings.Favorites, Storage.QueueSave);
        _recent = new ADTSpawnIdList(Settings.Recent, Storage.QueueSave, RecentLimit);

        _placement.PlacementChanged += OnPlacementChanged;
        _placement.DirectionChanged += OnDirectionChanged;
        _placement.MirroredChanged += OnMirroredChanged;
        _prototypes.PrototypesReloaded += OnPrototypesReloaded;
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
        UpdateRotationState();
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

        _window = UIManager.CreateWindow<ADTTileSpawnWindow>();

        _window.IsFavorite = _favorites.Contains;
        _window.OnClose += OnWindowClosed;
        _window.OnSearchChanged += OnSearchChanged;
        _window.OnCategorySelected += OnCategorySelected;
        _window.OnTileSelected += OnTileSelected;
        _window.OnFavoriteToggled += OnFavoriteToggled;
        _window.EraseButton.Pressed = _eraseTile;
        _window.EraseButton.OnToggled += OnEraseToggled;
        _window.MirroredButton.Pressed = _placement.Mirrored;
        _window.MirroredButton.OnToggled += OnMirroredToggled;
        _window.OldWindowButton.OnPressed += OnOldWindowPressed;
        _window.SetGridMode(Settings.Grid);
        ADTSpawnIdList.LoadSize(Settings.Size, _window);

        _search = string.Empty;
        _indexDirty = true;
    }

    private void RebuildIndex()
    {
        if (_window == null || _window.Disposed)
            return;

        _indexDirty = false;
        _index ??= new ADTTileSpawnIndex();
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
        _shown.Clear();

        foreach (var entry in _index.Entries)
        {
            if (_search.Length > 0 && !entry.SearchKey.Contains(_search, StringComparison.Ordinal))
                continue;

            var favorite = _favorites.Contains(entry.Definition.ID);
            var recent = _recent.Contains(entry.Definition.ID);

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
                _shown.Add(entry);
            }
        }

        if (selected == _index.Recent)
        {
            _shown.Sort((a, b) =>
                _recent.IndexOf(a.Definition.ID).CompareTo(_recent.IndexOf(b.Definition.ID)));
        }

        _window.UpdateCategoryCounts(_search.Length > 0);
        _window.SetItems(_shown);
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

    private void OnFavoriteToggled(ADTTileSpawnEntry entry)
    {
        _favorites.Toggle(entry.Definition.ID);
        UpdateShown();
        _window?.RefreshDetails();
    }

    private void OnTileSelected(ADTTileSpawnEntry? entry)
    {
        if (_window == null || _window.Disposed)
            return;

        if (entry == null)
        {
            _window.SetSelected(null);
            _placement.Clear();
            return;
        }

        _eraseTile = false;
        StartTilePlacement(entry.Definition.TileId);
        _recent.Push(entry.Definition.ID);
        _window.SetSelected(entry);
        UpdateRotationState();
    }

    private void StartTilePlacement(int tileType)
    {
        var info = new PlacementInformation
        {
            PlacementOption = nameof(AlignTileAny),
            TileType = tileType,
            Range = 400,
            IsTile = true,
        };

        _placement.BeginPlacing(info);
    }

    private void OnEraseToggled(ButtonToggledEventArgs args)
    {
        if (_window == null || _window.Disposed)
            return;

        _placement.Clear();

        _eraseTile = args.Pressed;
        if (args.Pressed)
            StartTilePlacement(0);

        args.Button.Pressed = args.Pressed;
    }

    private void OnMirroredToggled(ButtonToggledEventArgs args)
    {
        _placement.Mirrored = args.Pressed;
        args.Button.Pressed = args.Pressed;
    }

    private void OnOldWindowPressed(ButtonEventArgs args)
    {
        CloseWindow();
        UIManager.GetUIController<TileSpawningUIController>().ToggleWindow();
    }

    private void OnWindowClosed()
    {
        if (_window == null || _window.Disposed)
            return;

        _window.SetSelected(null);
        _placement.Clear();

        Settings.Grid = _window.IsGrid;
        Settings.Size = _window.Size;
        Storage.QueueSave();
    }

    private void OnPlacementChanged(object? sender, EventArgs e)
    {
        if (_window == null || _window.Disposed)
            return;

        if (_placement.CurrentPermission is { IsTile: true })
            return;

        _window.SetSelected(null);
        _window.EraseButton.Pressed = false;
        _eraseTile = false;
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (!args.WasModified<ContentTileDefinition>())
            return;

        _indexDirty = true;

        if (_window is { Disposed: false, IsOpen: true })
            RebuildIndex();
    }

    private void OnDirectionChanged(object? sender, EventArgs e)
    {
        UpdateRotationState();
    }

    private void OnMirroredChanged(object? sender, EventArgs e)
    {
        UpdateRotationState();
    }

    private void UpdateRotationState()
    {
        if (_window == null || _window.Disposed)
            return;

        var allowed = false;
        if (_placement.CurrentPermission is { IsTile: true } permission && permission.TileType != 0)
            allowed = _tiles[permission.TileType].AllowRotationMirror;

        _window.MirroredButton.Disabled = !allowed;
        _window.MirroredButton.Pressed = _placement.Mirrored;
        _window.RotationLabel.Text = Loc.GetString("adt-spawn-menu-rotation", ("direction", _placement.Direction.ToString()));
        _window.RotationLabel.FontColorOverride = allowed ? Color.White : Color.Gray;
    }
}
