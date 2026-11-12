using Content.Client.ADT.UserData;
using Robust.Shared.ContentPack;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Utility;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace Content.Client.ADT.Drawing.Input;

public static class DrawingHotkeyStorage
{
    private static readonly ResPath SavePath = ADTUserData.Root / "canvas" / "hotkeys.yml";

    public static void Load(DrawingHotkeyMap map, IResourceManager resources, ISerializationManager serialization, ISawmill sawmill)
    {
        if (!resources.UserData.Exists(SavePath))
            return;

        try
        {
            using var reader = resources.UserData.OpenText(SavePath);
            var yaml = new YamlStream();
            yaml.Load(reader);

            var data = serialization.Read<DrawingHotkeyData>(
                yaml.Documents[0].RootNode.ToDataNode(),
                notNullableOverride: true);

            map.Import(data);
        }
        catch (Exception e)
        {
            sawmill.Error($"Failed to load drawing hotkeys: {e}");
        }
    }

    public static void Save(DrawingHotkeyMap map, IResourceManager resources, ISerializationManager serialization, ISawmill sawmill)
    {
        try
        {
            resources.UserData.CreateDir(SavePath.Directory);

            var node = serialization.WriteValue(map.Export(), notNullableOverride: true);
            using var writer = resources.UserData.OpenWriteText(SavePath);
            var yaml = new YamlStream { new YamlDocument(node.ToYamlNode()) };
            yaml.Save(new YamlMappingFix(new Emitter(writer)), false);
        }
        catch (Exception e)
        {
            sawmill.Error($"Failed to save drawing hotkeys: {e}");
        }
    }
}

[DataDefinition]
public sealed partial class DrawingHotkeyData
{
    [DataField]
    public List<DrawingHotkeyEntry> Hotkeys = new();
}

[DataDefinition]
public sealed partial class DrawingHotkeyEntry
{
    [DataField]
    public DrawingAction Action;

    [DataField]
    public List<string> Keys = new();
}
