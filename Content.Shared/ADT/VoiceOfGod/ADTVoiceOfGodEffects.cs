using Content.Shared.ADT.Hallucinations;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.EntityEffects;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.VoiceOfGod;

public sealed partial class ADTStun : EntityEffectBase<ADTStun>
{
    [DataField]
    public TimeSpan Time = TimeSpan.FromSeconds(2);
}

public sealed partial class ADTKnockdown : EntityEffectBase<ADTKnockdown>
{
    [DataField]
    public TimeSpan Time = TimeSpan.FromSeconds(2);

    [DataField]
    public bool Scaled = true;

    [DataField]
    public bool AutoStand = true;

    [DataField]
    public bool Drop = true;
}

public sealed partial class ADTStandUp : EntityEffectBase<ADTStandUp>;

public sealed partial class ADTWakeUp : EntityEffectBase<ADTWakeUp>
{
    [DataField(required: true)]
    public EntProtoId SleepEffect;
}

public sealed partial class ADTForceVomit : EntityEffectBase<ADTForceVomit>;

public sealed partial class ADTHallucinate : EntityEffectBase<ADTHallucinate>
{
    [DataField(required: true)]
    public string Key = string.Empty;

    [DataField(required: true)]
    public ProtoId<HallucinationsPrototype> Pack;

    [DataField]
    public TimeSpan Time = TimeSpan.FromSeconds(15);
}

public sealed partial class ADTDamageFromUser : EntityEffectBase<ADTDamageFromUser>
{
    [DataField]
    public DamageSpecifier? Damage;

    [DataField]
    public Dictionary<ProtoId<DamageGroupPrototype>, float> Groups = new();

    [DataField]
    public bool IgnoreResistances;
}

public sealed partial class ADTFireStacks : EntityEffectBase<ADTFireStacks>
{
    [DataField]
    public float Stacks = 1f;
}

public sealed partial class ADTRepulseFromUser : EntityEffectBase<ADTRepulseFromUser>
{
    [DataField]
    public float Distance = 3f;

    [DataField]
    public float Speed = 10f;
}

public sealed partial class ADTRandomStep : EntityEffectBase<ADTRandomStep>
{
    [DataField]
    public float Speed = 4f;
}

public sealed partial class ADTForceSay : EntityEffectBase<ADTForceSay>
{
    [DataField(required: true)]
    public LocId Message;
}

public sealed partial class ADTStateLaws : EntityEffectBase<ADTStateLaws>;

public sealed partial class ADTForceEmote : EntityEffectBase<ADTForceEmote>
{
    [DataField(required: true)]
    public ProtoId<EmotePrototype> Emote;
}

public sealed partial class ADTSetCombatMode : EntityEffectBase<ADTSetCombatMode>
{
    [DataField]
    public bool Enabled;
}

public sealed partial class ADTBuckleNearby : EntityEffectBase<ADTBuckleNearby>
{
    [DataField]
    public float Range = 0.4f;
}

public sealed partial class ADTUnbuckle : EntityEffectBase<ADTUnbuckle>;
