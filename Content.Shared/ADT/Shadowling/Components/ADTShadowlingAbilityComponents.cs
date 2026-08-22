using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage;
using Content.Shared.Polymorph;
using Content.Shared.StatusEffect;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Shadowling;

[RegisterComponent, NetworkedComponent]
public sealed partial class ADTShadowlingGlareActionComponent : Component
{
    [DataField]
    public float MeleeRange = 2f;

    [DataField]
    public TimeSpan CloseKnockdown = TimeSpan.FromSeconds(4);

    [DataField]
    public TimeSpan CloseMute = TimeSpan.FromSeconds(20);

    [DataField]
    public float CloseStamina = 20f;

    [DataField]
    public TimeSpan FarStun = TimeSpan.FromSeconds(2);

    [DataField]
    public TimeSpan FarSlow = TimeSpan.FromSeconds(10);

    [DataField]
    public TimeSpan FarMute = TimeSpan.FromSeconds(10);

    [DataField]
    public ProtoId<StatusEffectPrototype> MuteEffect = "Muted";

    [DataField]
    public ProtoId<StatusEffectPrototype> SlowEffect = "SlowedDown";

    [DataField]
    public SoundSpecifier? Sound;
}

[RegisterComponent, NetworkedComponent]
public sealed partial class ADTShadowlingVeilActionComponent : Component
{
    [DataField]
    public float Range = 5f;

    [DataField]
    public SoundSpecifier? Sound;
}

[RegisterComponent, NetworkedComponent]
public sealed partial class ADTShadowlingShadowWalkActionComponent : Component
{
    [DataField]
    public ProtoId<PolymorphPrototype> Polymorph = "ADTShadowlingShadowWalk";

    [DataField]
    public EntProtoId? EnterEffect = "ADTEffectShadowWalkEnter";

    [DataField]
    public SoundSpecifier? Sound;
}

[RegisterComponent, NetworkedComponent]
public sealed partial class ADTShadowlingIcyVeinsActionComponent : Component
{
    [DataField]
    public float Range = 5f;

    [DataField]
    public TimeSpan Stun = TimeSpan.FromSeconds(2);

    [DataField]
    public ProtoId<ReagentPrototype> Reagent = "ADTFrostoil";

    [DataField]
    public float ReagentAmount = 15f;

    [DataField]
    public DamageSpecifier Damage = new()
    {
        DamageDict = new()
        {
            ["Cold"] = 10,
        },
    };

    [DataField]
    public SoundSpecifier? Sound;
}

[RegisterComponent, NetworkedComponent]
public sealed partial class ADTShadowlingGuiseActionComponent : Component
{
    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(4);

    [DataField]
    public float Visibility = -0.9f;

    [DataField]
    public SoundSpecifier? Sound;
}
