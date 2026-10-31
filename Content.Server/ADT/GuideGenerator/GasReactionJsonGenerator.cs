using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Content.Server.Atmos.Reactions;
using Content.Shared.ADT.Atmos.Prototypes;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.GuideGenerator;

public sealed class GasReactionJsonGenerator
{
    public static void PublishJson(StreamWriter file)
    {
        var prototype = IoCManager.Resolve<IPrototypeManager>();

        var reactions = prototype
            .EnumeratePrototypes<GasReactionPrototype>()
            .Select(x => new GasReactionEntry(x, prototype.Index<GasReactionGuidePrototype>(x.ID)))
            .ToDictionary(x => x.Id, x => x);

        var serializeOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
        };

        file.Write(JsonSerializer.Serialize(reactions, serializeOptions));
    }
}

public sealed class GasReactionEntry
{
    [JsonPropertyName("id")]
    public string Id { get; }

    [JsonPropertyName("name")]
    public string Name { get; }

    [JsonPropertyName("priority")]
    public int Priority { get; }

    [JsonPropertyName("minTemp")]
    public float? MinTemp { get; }

    [JsonPropertyName("maxTemp")]
    public float? MaxTemp { get; }

    [JsonPropertyName("requirements")]
    public Dictionary<string, float> Requirements { get; } = new();

    [JsonPropertyName("consumes")]
    public Dictionary<string, float> Consumes { get; }

    [JsonPropertyName("produces")]
    public Dictionary<string, float> Produces { get; }

    [JsonPropertyName("catalysts")]
    public List<string> Catalysts { get; }

    [JsonPropertyName("notes")]
    public List<string> Notes { get; } = new();

    public GasReactionEntry(GasReactionPrototype proto, GasReactionGuidePrototype guide)
    {
        Id = proto.ID;
        Name = Loc.GetString(guide.Name);
        Priority = proto.Priority;

        if (proto.MinimumTemperatureRequirement > Atmospherics.TCMB)
            MinTemp = proto.MinimumTemperatureRequirement;

        if (proto.MaximumTemperatureRequirement < float.MaxValue)
            MaxTemp = proto.MaximumTemperatureRequirement;

        foreach (var gas in Enum.GetValues<Gas>())
        {
            var moles = proto.MinimumRequirements[(int) gas];

            if (moles > 0)
                Requirements[gas.ToString()] = moles;
        }

        Consumes = ToGasIds(guide.Consumes);
        Produces = ToGasIds(guide.Produces);
        Catalysts = Requirements.Keys.Where(x => !Consumes.ContainsKey(x)).ToList();

        if (guide.Note is { } note)
            Notes.Add(Loc.GetString(note));

        if (guide.Heat == GasReactionHeat.Released)
            Notes.Add(Loc.GetString("gas-reaction-guide-heat-released"));

        if (guide.Heat == GasReactionHeat.Absorbed)
            Notes.Add(Loc.GetString("gas-reaction-guide-heat-absorbed"));

        if (guide.NobliumInhibited)
            Notes.Add(Loc.GetString("gas-reaction-guide-noblium-inhibited"));
    }

    private static Dictionary<string, float> ToGasIds(Dictionary<ProtoId<GasPrototype>, float> gases)
    {
        return gases.ToDictionary(x => x.Key.Id, x => x.Value);
    }
}
