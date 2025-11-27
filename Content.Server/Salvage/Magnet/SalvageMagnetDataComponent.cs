using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Salvage.Magnet;

/// <summary>
/// Added to the station to hold salvage magnet data.
/// </summary>
[RegisterComponent]
public sealed partial class SalvageMagnetDataComponent : Component
{
    // May be multiple due to splitting.

    /// <summary>
    /// Entities currently magnetised.
    /// </summary>
    [DataField]
    public List<EntityUid>? ActiveEntities;

    /// <summary>
    /// If the magnet is currently active when does it end.
    /// </summary>
    [DataField(customTypeSerializer:typeof(TimeOffsetSerializer))]
    public TimeSpan? EndTime;

    [DataField(customTypeSerializer:typeof(TimeOffsetSerializer))]
    public TimeSpan NextOffer;

    /// <summary>
    /// How long salvage will be active for before despawning.
    /// </summary>
    [DataField]
    public TimeSpan ActiveTime = TimeSpan.FromMinutes(6);

    /// <summary>
    /// Cooldown between offerings after one ends.
    /// </summary>
    [DataField]
    public TimeSpan OfferCooldown = TimeSpan.FromMinutes(3);

    /// <summary>
    /// Seeds currently offered
    /// </summary>
    [DataField]
    public List<int> Offered = new();

    [DataField]
    public int OfferCount = 5;

    [DataField]
    public int ActiveSeed;

    /// <summary>
    /// Final countdown announcement.
    /// </summary>
    [DataField]
    public bool Announced;

    // ADT-Tweak-Start: Настраиваемое время для разных размеров фрагментов магнита

    [DataField]
    public Dictionary<string, TimeSpan> sizeAndTime=new Dictionary<string, TimeSpan>
    {
        {"SmallMagnetTargets",TimeSpan.FromMinutes(2)},
        {"OreMagnetTargets",TimeSpan.FromMinutes(3)},
        {"MediumMagnetTargets",TimeSpan.FromMinutes(4)},
        {"BigMagnetTargets",TimeSpan.FromMinutes(6)},
        {"ErrorTime",TimeSpan.FromMinutes(6)}
    };
    // ADT-Tweak-End
}
