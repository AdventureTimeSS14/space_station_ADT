using System.IO;
using System.Linq;
using Robust.Shared.ContentPack;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace Content.Client.ADT.Actions;

public sealed class ADTActionOrderSystem : EntitySystem
{
    private static readonly ResPath SavePath = new("/adt_action_order.yml");
    private static readonly TimeSpan SaveDelay = TimeSpan.FromSeconds(1);

    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private IResourceManager _resources = default!;
    [Dependency] private ISerializationManager _serialization = default!;

    private readonly ADTActionOrder _order = new();

    private TimeSpan? _saveAt;

    public override void Initialize()
    {
        base.Initialize();

        Load();
    }

    public override void Shutdown()
    {
        base.Shutdown();

        if (_saveAt != null)
            Save();
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        if (_saveAt is { } saveAt && _timing.RealTime >= saveAt)
            Save();
    }

    public bool HasPlace(EntProtoId action)
    {
        return _order.HasPlace(action);
    }

    public bool IsRemoved(EntProtoId action)
    {
        return _order.IsRemoved(action);
    }

    public List<T> Arrange<T>(IEnumerable<T> actions, Func<T, EntProtoId?> getKey)
    {
        return _order.Arrange(actions, getKey);
    }

    public int GetInsertIndex(IReadOnlyList<EntProtoId?> hotbar, EntProtoId action)
    {
        return _order.GetInsertIndex(hotbar, action);
    }

    public void Store(IEnumerable<EntProtoId?> hotbar)
    {
        if (_order.Store(hotbar))
            QueueSave();
    }

    public void SetRemoved(EntProtoId action, bool removed)
    {
        if (_order.SetRemoved(action, removed))
            QueueSave();
    }

    private void QueueSave()
    {
        _saveAt ??= _timing.RealTime + SaveDelay;
    }

    private void Load()
    {
        if (!_resources.UserData.Exists(SavePath))
            return;

        try
        {
            using var reader = _resources.UserData.OpenText(SavePath);
            var yaml = new YamlStream();
            yaml.Load(reader);

            var data = _serialization.Read<ADTActionOrderData>(yaml.Documents[0].RootNode.ToDataNode(), notNullableOverride: true);

            _order.Load(data.Order.Where(id => _prototype.HasIndex(id)), data.Removed.Where(id => _prototype.HasIndex(id)));
        }
        catch (Exception e)
        {
            Log.Error($"Failed to load the action order: {e}");
        }
    }

    private void Save()
    {
        _saveAt = null;

        var data = new ADTActionOrderData
        {
            Order = _order.Order.ToList(),
            Removed = _order.Removed.ToList(),
        };

        try
        {
            var node = _serialization.WriteValue(data, notNullableOverride: true);
            using var writer = _resources.UserData.OpenWriteText(SavePath);
            var yaml = new YamlStream { new YamlDocument(node.ToYamlNode()) };
            yaml.Save(new YamlMappingFix(new Emitter(writer)), false);
        }
        catch (Exception e)
        {
            Log.Error($"Failed to save the action order: {e}");
        }
    }
}

[DataDefinition]
public sealed partial class ADTActionOrderData
{
    [DataField]
    public List<EntProtoId> Order = new();

    [DataField]
    public List<EntProtoId> Removed = new();
}
