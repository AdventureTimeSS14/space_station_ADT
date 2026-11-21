using Content.Shared.ADT.Chaplain.Prayers;
using Content.Shared.FixedPoint;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Chaplain.Prototypes;

[Prototype]
public sealed partial class ChaplainPrayerPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public string Text = string.Empty;

    [DataField]
    public ChaplainPrayerRole Role = ChaplainPrayerRole.Chaplain;

    [DataField]
    public FixedPoint2 Cost;

    [DataField]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(30);

    [DataField]
    public float ChaplainRange;

    [DataField]
    public EntityWhitelist? Whitelist;

    [DataField]
    public EntProtoId? Effect;

    [DataField]
    public SoundSpecifier? Sound;

    [DataField(required: true)]
    public List<ChaplainPrayerEffect> Effects = new();
}

public enum ChaplainPrayerRole : byte
{
    Chaplain,
    Follower,
}
