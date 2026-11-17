using Content.Shared.ADT.Chaplain.Prototypes;
using Content.Shared.ADT.Damage;
using Content.Shared.Alert;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.StatusIcon;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.ADT.Chaplain.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class ChaplainComponent : Component
{
    [DataField, AutoNetworkedField]
    public FixedPoint2 Faith = 5;

    [DataField, AutoNetworkedField]
    public FixedPoint2 MaxFaith = 10;

    [DataField]
    public FixedPoint2 BaseRegen = 0.5;

    [DataField]
    public FixedPoint2 FollowerRegen = 0.25;

    [DataField]
    public TimeSpan RegenDelay = TimeSpan.FromSeconds(30);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextRegen;

    [DataField]
    public float WeakenedHolyMultiplier = 0.5f;

    [DataField, AutoNetworkedField]
    public List<EntityUid> Followers = new();

    [DataField, AutoNetworkedField]
    public int MaxFollowers = 3;

    [DataField]
    public FixedPoint2 BelieverCost = 3;

    [DataField]
    public TimeSpan MakeBelieverDuration = TimeSpan.FromSeconds(7);

    [DataField]
    public FixedPoint2 ShieldCost = 5;

    [DataField]
    public TimeSpan ShieldDuration = TimeSpan.FromSeconds(60);

    [DataField]
    public SoundSpecifier ShieldSound = new SoundPathSpecifier("/Audio/ADT/Chaplain/holy_shield.ogg");

    [DataField]
    public FixedPoint2 ExorcismCost = 3;

    [DataField]
    public DamageSpecifier ExorcismDamage = new()
    {
        DamageDict = new()
        {
            { "Holy", 15 },
        },
    };

    [DataField]
    public FixedPoint2 ExorcismSuppression = 25;

    [DataField]
    public TimeSpan ExorcismSlowdown = TimeSpan.FromSeconds(4);

    [DataField]
    public float ExorcismSlowdownMultiplier = 0.5f;

    [DataField]
    public SoundSpecifier ExorcismSound = new SoundPathSpecifier("/Audio/ADT/Chaplain/exorcism.ogg");

    [DataField]
    public FixedPoint2 BlessingCost = 2;

    [DataField]
    public GroupHealSpecifier BlessingHeal = new()
    {
        GroupHealDict = new()
        {
            { "Brute", -15 },
            { "Burn", -15 },
        },
    };

    [DataField]
    public SoundSpecifier BlessingSound = new SoundPathSpecifier("/Audio/ADT/Chaplain/blessing.ogg");

    [DataField]
    public FixedPoint2 HolyWaterCost = 2;

    [DataField]
    public SoundSpecifier HolyWaterSoundPath = new SoundPathSpecifier("/Audio/Effects/holy.ogg");

    [DataField]
    public ProtoId<ReagentPrototype> WaterSolution = "Water";

    [DataField]
    public ProtoId<ReagentPrototype> WaterReplaceSolution = "Holywater";

    [DataField]
    public ProtoId<ReagentPrototype> BloodSolution = "Blood";

    [DataField]
    public ProtoId<ReagentPrototype> BloodReplaceSolution = "Wine";

    [DataField]
    public FixedPoint2 RevenantDamage = 50;

    [DataField]
    public FixedPoint2 RevenantSelfDamage = 10;

    [DataField, AutoNetworkedField]
    public EntityUid? BelieverActionEntity;

    [DataField, AutoNetworkedField]
    public EntityUid? ShieldActionEntity;

    [DataField, AutoNetworkedField]
    public EntityUid? HolyTouchActionEntity;

    [DataField, AutoNetworkedField]
    public EntityUid? ExorcismActionEntity;

    [DataField, AutoNetworkedField]
    public EntityUid? BlessingActionEntity;

    [ViewVariables]
    public Dictionary<ProtoId<ChaplainPrayerPrototype>, TimeSpan> PrayerCooldowns = new();

    [DataField]
    public ProtoId<FactionIconPrototype> StatusIcon = "BelieverFaction";

    [DataField]
    public bool IconVisibleToGhost = true;

    [DataField]
    public ProtoId<AlertPrototype> Alert = "ADTChaplain";
}
