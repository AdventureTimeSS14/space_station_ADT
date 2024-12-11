using Content.Shared.Alert;
using Robust.Shared.Prototypes;
using Robust.Shared.Audio;

namespace Content.Shared.ADT.Crawling;

[RegisterComponent]
public sealed partial class CrawlerComponent : Component
{
    /// <summary>
    ///     The time for getting up's doafter
    /// </summary>
    [DataField]
    public TimeSpan StandUpTime = TimeSpan.FromSeconds(1.5);

    /// <summary>
    ///     The explosive resistance coefficient, This fraction is multiplied into the total resistance if player downed.
    /// </summary>
    [DataField("downeddamageCoefficient")]
    public float DownedDamageCoefficient = 0.2F;

    [DataField]
    public SoundCollectionSpecifier TableBonkSound = new SoundCollectionSpecifier("TrayHit");

    [DataField]
    public TimeSpan DefaultStunTime = TimeSpan.FromSeconds(2.5);

    [DataField]
    public ProtoId<AlertPrototype> CtawlingAlert = "Crawling";
}
