using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Content.Server.GuideGenerator;
using Content.Shared.Atmos.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.GuideGenerator;

public sealed class GasJsonGenerator
{
    public static void PublishJson(StreamWriter file)
    {
        var prototype = IoCManager.Resolve<IPrototypeManager>();

        var gases = prototype
            .EnumeratePrototypes<GasPrototype>()
            .Select(x => new GasEntry(x, prototype))
            .ToDictionary(x => x.Id, x => x);

        var serializeOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
            Converters =
            {
                new ChemistryJsonGenerator.FixedPointJsonConverter(),
            },
        };

        file.Write(JsonSerializer.Serialize(gases, serializeOptions));
    }
}

public sealed class GasEntry
{
    [JsonPropertyName("id")]
    public string Id { get; }

    [JsonPropertyName("name")]
    public string Name { get; }

    [JsonPropertyName("abbreviation")]
    public string Abbreviation { get; }

    [JsonPropertyName("group")]
    public string Group { get; } = "Gases";

    [JsonPropertyName("desc")]
    public string Description { get; } = string.Empty;

    [JsonPropertyName("physicalDesc")]
    public string PhysicalDescription { get; } = string.Empty;

    [JsonPropertyName("color")]
    public string SubstanceColor { get; }

    [JsonPropertyName("textColor")]
    public string TextColor { get; }

    [JsonPropertyName("gasColor")]
    public string GasColor { get; }

    [JsonPropertyName("specificHeat")]
    public float SpecificHeat { get; }

    [JsonPropertyName("heatCapacityRatio")]
    public float HeatCapacityRatio { get; }

    [JsonPropertyName("molarMass")]
    public float MolarMass { get; }

    [JsonPropertyName("pricePerMole")]
    public float PricePerMole { get; }

    [JsonPropertyName("isFuel")]
    public bool IsFuel { get; }

    [JsonPropertyName("isOxidizer")]
    public bool IsOxidizer { get; }

    [JsonPropertyName("reagentId")]
    public string? ReagentId { get; }

    [JsonPropertyName("metabolisms")]
    public Dictionary<string, Corvax.GuideGenerator.ReagentEffectsEntry>? Metabolisms { get; }

    public GasEntry(GasPrototype proto, IPrototypeManager prototype)
    {
        Id = proto.ID;
        Name = Loc.GetString(proto.Name);
        Abbreviation = Loc.GetString(proto.Abbreviation);
        SubstanceColor = proto.Color.ToHexNoAlpha();
        GasColor = SubstanceColor;
        SpecificHeat = proto.MolarHeatCapacity;
        HeatCapacityRatio = proto.HeatCapacityRatio;
        MolarMass = proto.MolarMass;
        PricePerMole = proto.PricePerMole;
        IsFuel = proto.IsFuel;
        IsOxidizer = proto.IsOxidizer;

        var luminance = 0.2126f * proto.Color.R + 0.7152f * proto.Color.G + 0.0722f * proto.Color.B;
        TextColor = (luminance > 0.5f ? Color.Black : Color.White).ToHexNoAlpha();

        if (proto.Reagent is not { } reagentId)
            return;

        var reagent = new ReagentEntry(prototype.Index(reagentId));
        ReagentId = reagent.Id;
        Description = reagent.Description;
        PhysicalDescription = reagent.PhysicalDescription;
        Metabolisms = reagent.Metabolisms;
    }
}
