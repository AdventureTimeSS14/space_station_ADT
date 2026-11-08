using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using Robust.Shared.Prototypes;

namespace Content.Server.GuideGenerator;

public sealed class EntityJsonGenerator
{
    public static void PublishJson(StreamWriter file)
    {
        var prototype = IoCManager.Resolve<IPrototypeManager>();
        var prototypes =
            prototype
                .EnumeratePrototypes<EntityPrototype>()
                .Where(x => !x.Abstract)
                .Select(x => new EntityEntry(x))
                .ToDictionary(x => x.Id, x => x);

        var serializeOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All), // ADT-Tweak
        };

        file.Write(JsonSerializer.Serialize(prototypes, serializeOptions));
    }
}
