using Content.Shared.Alert;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Crawling;

[RegisterComponent]
public sealed partial class CrawlerComponent : Component
{
    /// <summary>
    ///     The time for getting up's doafter
    /// </summary>
    [DataField]
    public TimeSpan StandUpTime = TimeSpan.FromSeconds(1);

    /// <summary>
    ///     The explosive resistance coefficient, This fraction is multiplied into the total resistance if player downed.
    /// </summary>
    [DataField("downeddamageCoefficient")]
    public float DownedDamageCoefficient = 0.2F;

    [DataField]
    public ProtoId<AlertPrototype> CrawlingAlert = "Crawling";
}
