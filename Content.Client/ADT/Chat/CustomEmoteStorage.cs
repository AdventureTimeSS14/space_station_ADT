using System.Linq;
using Robust.Shared.ContentPack;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Utility;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace Content.Client.ADT.Chat;

/// <summary>
/// Пользовательские эмоуты лежат в аппдате: /ADT/custom_emotes.yml.
/// Окно настройки работает со строками "триггер=эмоут", на диске они хранятся парами.
/// </summary>
public static class CustomEmoteStorage
{
    private static readonly ResPath SaveDir = new("/ADT");
    private static readonly ResPath SavePath = new("/ADT/custom_emotes.yml");

    public static string Load(IResourceManager resources, ISerializationManager serialization, ISawmill sawmill)
    {
        if (!resources.UserData.Exists(SavePath))
            return string.Empty;

        try
        {
            using var reader = resources.UserData.OpenText(SavePath);
            var yaml = new YamlStream();
            yaml.Load(reader);

            var data = serialization.Read<CustomEmoteData>(
                yaml.Documents[0].RootNode.ToDataNode(),
                notNullableOverride: true);

            return string.Join('\n', data.Emotes.Select(e => $"{e.Trigger}={e.Emote}"));
        }
        catch (Exception e)
        {
            sawmill.Error($"Failed to load custom emotes: {e}");
            return string.Empty;
        }
    }

    public static void Save(
        IResourceManager resources,
        ISerializationManager serialization,
        ISawmill sawmill,
        string raw)
    {
        var data = new CustomEmoteData
        {
            Emotes = CustomEmoteParser.ParseEntries(raw)
                .Select(e => new CustomEmoteEntry { Trigger = e.Trigger, Emote = e.Emote })
                .ToList(),
        };

        try
        {
            resources.UserData.CreateDir(SaveDir);

            var node = serialization.WriteValue(data, notNullableOverride: true);
            using var writer = resources.UserData.OpenWriteText(SavePath);
            var yaml = new YamlStream { new YamlDocument(node.ToYamlNode()) };
            yaml.Save(new YamlMappingFix(new Emitter(writer)), false);
        }
        catch (Exception e)
        {
            sawmill.Error($"Failed to save custom emotes: {e}");
        }
    }
}

[DataDefinition]
public sealed partial class CustomEmoteData
{
    [DataField]
    public List<CustomEmoteEntry> Emotes = new();
}

[DataDefinition]
public sealed partial class CustomEmoteEntry
{
    [DataField(required: true)]
    public string Trigger = string.Empty;

    [DataField(required: true)]
    public string Emote = string.Empty;
}
