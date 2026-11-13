using System.Numerics;
using JetBrains.Annotations;
using Content.Client.ADT.UserData;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.ContentPack;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Utility;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Client.ADT.SpawnMenu;

[UsedImplicitly]
public sealed class ADTSpawnMenuStorageUIController : UIController
{
    [Dependency] private IResourceManager _resources = default!;
    [Dependency] private ISerializationManager _serialization = default!;
    [Dependency] private ILogManager _log = default!;

    private static readonly ResPath LegacySavePath = new("/adt_spawn_menu.yml");
    private static readonly ResPath SavePath = ADTUserData.Root / "spawn_menu.yml";
    private const int SaveDelay = 1000;

    private ADTSpawnMenuData? _data;
    private bool _savePending;

    public ADTSpawnMenuData Data
    {
        get
        {
            if (_data == null)
                Load();

            return _data!;
        }
    }

    public void QueueSave()
    {
        if (_savePending)
            return;

        _savePending = true;
        Timer.Spawn(SaveDelay, Save);
    }

    private void Load()
    {
        _data = new ADTSpawnMenuData();

        try
        {
            ADTUserData.MoveLegacy(_resources.UserData, LegacySavePath, SavePath);
            if (!_resources.UserData.Exists(SavePath))
                return;

            using var reader = _resources.UserData.OpenText(SavePath);
            var yaml = new YamlStream();
            yaml.Load(reader);

            _data = _serialization.Read<ADTSpawnMenuData>(yaml.Documents[0].RootNode.ToDataNode(), notNullableOverride: true);
        }
        catch (Exception e)
        {
            _log.GetSawmill("adt.spawnmenu").Error($"Failed to load spawn menu settings: {e}");
        }
    }

    private void Save()
    {
        _savePending = false;

        if (_data == null)
            return;

        try
        {
            var node = _serialization.WriteValue(_data, notNullableOverride: true);
            _resources.UserData.CreateDir(SavePath.Directory);
            using var writer = _resources.UserData.OpenWriteText(SavePath);
            var yaml = new YamlStream { new YamlDocument(node.ToYamlNode()) };
            yaml.Save(new YamlMappingFix(new Emitter(writer)), false);
        }
        catch (Exception e)
        {
            _log.GetSawmill("adt.spawnmenu").Error($"Failed to save spawn menu settings: {e}");
        }
    }
}

[DataDefinition]
public sealed partial class ADTSpawnMenuData
{
    [DataField]
    public ADTSpawnWindowData Entities = new();

    [DataField]
    public ADTSpawnWindowData Tiles = new()
    {
        Grid = true,
    };

    [DataField]
    public ADTSpawnWindowData Decals = new()
    {
        Grid = true,
    };
}

[DataDefinition]
public sealed partial class ADTSpawnWindowData
{
    [DataField]
    public List<string> Favorites = new();

    [DataField]
    public List<string> Recent = new();

    [DataField]
    public bool Grid;

    [DataField]
    public bool GroupVariants = true;

    [DataField]
    public bool ShowService;

    [DataField]
    public bool Details = true;

    [DataField]
    public string Category = string.Empty;

    [DataField]
    public Vector2? Size;

    [DataField]
    public List<Color> RecentColors = new();
}
