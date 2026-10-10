using Robust.Shared.Prototypes;

namespace Content.Server.ADT.InconnuOS.NanoNet;

[Prototype("nanoNetSite")]
public sealed partial class NanoNetSitePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public string Host = default!;

    [DataField]
    public string? Scheme;

    [DataField]
    public string? Style;

    [DataField(required: true)]
    public Dictionary<string, NanoNetPage> Pages = new();
}

[DataDefinition]
public sealed partial class NanoNetPage
{
    [DataField(required: true)]
    public LocId Title;

    [DataField(required: true)]
    public LocId Body;

    [DataField]
    public List<NanoNetLink> Links = new();
}

[DataDefinition]
public sealed partial class NanoNetLink
{
    [DataField(required: true)]
    public LocId Text;

    [DataField(required: true)]
    public string Url = default!;
}
