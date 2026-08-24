using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage;
using Content.Shared.Polymorph;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Shadowling;

[RegisterComponent, NetworkedComponent]
public sealed partial class ADTShadowlingCollectiveMindActionComponent : Component
{
    [DataField]
    public TimeSpan CastTime = TimeSpan.FromSeconds(3);

    [DataField]
    public DamageSpecifier SelfHeal = new()
    {
        DamageDict = new()
        {
            ["Cellular"] = -50,
        },
    };

    [DataField]
    public SoundSpecifier? Sound;
}

[RegisterComponent, NetworkedComponent]
public sealed partial class ADTShadowlingBlindnessSmokeActionComponent : Component
{
    [DataField]
    public EntProtoId SmokeProto = "Smoke";

    [DataField]
    public ProtoId<ReagentPrototype> Reagent = "ADTShadowlingSmoke";

    [DataField]
    public float ReagentAmount = 100f;

    [DataField]
    public float Duration = 10f;

    [DataField]
    public int SpreadAmount = 20;

    [DataField]
    public SoundSpecifier? Sound;
}

[RegisterComponent, NetworkedComponent]
public sealed partial class ADTShadowlingScreechActionComponent : Component
{
    [DataField]
    public float Range = 5f;

    [DataField]
    public EntProtoId ConfusionEffect = "StatusEffectDrunk";

    [DataField]
    public TimeSpan ConfusionTime = TimeSpan.FromSeconds(20);

    [DataField]
    public float Deafness = 3f;

    [DataField]
    public float StaminaDamage = 30f;

    [DataField]
    public TimeSpan SiliconShutdown = TimeSpan.FromSeconds(12);

    [DataField]
    public DamageSpecifier WindowDamage = new()
    {
        DamageDict = new()
        {
            ["Blunt"] = 90,
        },
    };

    [DataField]
    public SoundSpecifier? Sound;
}

[RegisterComponent, NetworkedComponent]
public sealed partial class ADTShadowlingNullChargeActionComponent : Component
{
    [DataField]
    public SoundSpecifier? Sound;
}

[RegisterComponent, NetworkedComponent]
public sealed partial class ADTShadowlingBlackRecuperationActionComponent : Component
{
    [DataField]
    public TimeSpan EmpowerTime = TimeSpan.FromSeconds(8);

    [DataField]
    public TimeSpan ReviveTime = TimeSpan.FromSeconds(3);

    [DataField]
    public ProtoId<PolymorphPrototype> EmpowerPolymorph = "ADTShadowlingEmpowered";

    [DataField]
    public int MaxEmpowered = 5;

    [DataField]
    public bool IgnoreLimit;

    [DataField]
    public SoundSpecifier? Sound;
}

[RegisterComponent, NetworkedComponent]
public sealed partial class ADTShadowlingDestroyEnginesActionComponent : Component
{
    [DataField]
    public TimeSpan CastTime = TimeSpan.FromSeconds(15);

    [DataField]
    public TimeSpan ShuttleDelay = TimeSpan.FromMinutes(10);

    [DataField]
    public SoundSpecifier? Sound;
}
