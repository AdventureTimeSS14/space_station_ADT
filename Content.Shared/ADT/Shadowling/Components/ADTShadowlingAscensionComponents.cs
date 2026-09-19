using Content.Shared.Damage;
using Content.Shared.Polymorph;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Shadowling;

[RegisterComponent, NetworkedComponent]
public sealed partial class ADTShadowlingAscendActionComponent : Component
{
    [DataField]
    public List<TimeSpan> Stages = new()
    {
        TimeSpan.FromSeconds(5),
        TimeSpan.FromSeconds(10),
        TimeSpan.FromSeconds(9),
        TimeSpan.FromSeconds(6),
    };

    [DataField]
    public ProtoId<PolymorphPrototype> Polymorph = "ADTShadowlingAscended";

    [DataField]
    public float ShockwaveRange = 7f;

    [DataField]
    public TimeSpan ShockwaveKnockdown = TimeSpan.FromSeconds(20);

    [DataField]
    public bool BreakAllLights = true;

    [DataField]
    public SoundSpecifier? Sound = new SoundPathSpecifier("/Audio/ADT/Shadowling/hilarious_agony.ogg");

    [DataField]
    public LocId Announcement = "shadowling-ascension-announcement";

    [DataField]
    public LocId AnnouncementSender = "shadowling-ascension-announcer";
}

[RegisterComponent, NetworkedComponent]
public sealed partial class ADTAscendantAnnihilateActionComponent : Component
{
    [DataField]
    public SoundSpecifier? Sound = new SoundPathSpecifier("/Audio/ADT/Heretic/disintegrate.ogg");
}

[RegisterComponent, NetworkedComponent]
public sealed partial class ADTAscendantHypnosisActionComponent : Component
{
    [DataField]
    public SoundSpecifier? Sound = new SoundPathSpecifier("/Audio/ADT/hallucinations/veryfar_noise.ogg");
}

[RegisterComponent, NetworkedComponent]
public sealed partial class ADTAscendantPhaseShiftActionComponent : Component
{
    [DataField]
    public ProtoId<PolymorphPrototype> Polymorph = "ADTShadowlingAscendantPhased";

    [DataField]
    public SoundSpecifier? Sound = new SoundPathSpecifier("/Audio/ADT/Shadowling/bamf.ogg");
}

[RegisterComponent, NetworkedComponent]
public sealed partial class ADTAscendantLightningStormActionComponent : Component
{
    [DataField]
    public float Range = 6f;

    [DataField]
    public TimeSpan Knockdown = TimeSpan.FromSeconds(16);

    [DataField]
    public DamageSpecifier Damage = new()
    {
        DamageDict = new()
        {
            ["Shock"] = 50,
        },
    };

    [DataField]
    public SoundSpecifier? Sound = new SoundPathSpecifier("/Audio/Effects/Lightning/lightningbolt.ogg");
}

[RegisterComponent, NetworkedComponent]
public sealed partial class ADTAscendantBroadcastActionComponent : Component
{
    [DataField]
    public LocId Sender = "shadowling-broadcast-sender";

    [DataField]
    public SoundSpecifier? Sound = new SoundPathSpecifier("/Audio/ADT/hallucinations/veryfar_noise.ogg");
}
