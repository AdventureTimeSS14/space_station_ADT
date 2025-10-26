using Content.Shared.FixedPoint;
using Content.Shared.ADT.Sandevistan;
using Content.Shared.ADT.Trail;
using Content.Shared.ADT.Abilities;
using Content.Shared.Damage;
using Robust.Shared.Audio;

// Ideally speaking this should be on the heart itself... but this also works.
namespace Content.Server.ADT.Sandevistan;

[RegisterComponent]
public sealed partial class SandevistanUserComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    public ActiveSandevistanUserComponent? Active;

    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan? DisableAt;

    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan LastEnabled = TimeSpan.Zero;

    [DataField]
    public TimeSpan StatusEffectTime = TimeSpan.FromSeconds(5);

    [DataField]
    public TimeSpan PopupDelay = TimeSpan.FromSeconds(3);

    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan NextPopupTime = TimeSpan.Zero;

    [DataField]
    public string ActionProto = "ActionToggleSandevistan";

    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? ActionUid;

    [ViewVariables(VVAccess.ReadWrite)]
    public float CurrentLoad = 0f; // Only updated when enabled

    [DataField]
    public float LoadPerActiveSecond = 1f;

    [DataField]
    public float LoadPerInactiveSecond = -0.5f;

    [DataField]
    public SortedDictionary<SandevistanState, FixedPoint2> Thresholds = new()
    {
        { SandevistanState.Warning, 6 },
        { SandevistanState.Shaking, 12 },
        { SandevistanState.Stamina, 18 },
        { SandevistanState.Damage, 24 },
        { SandevistanState.Death, 32 },
    };

    [DataField]
    public float StaminaDamage = 10f;

    [DataField]
    public DamageSpecifier Damage = new()
    {
        DamageDict = new Dictionary<string, FixedPoint2>
        {
            { "Blunt", 10 },
        },
    };

    [DataField]
    public float MovementSpeedModifier = 2.4f;

    [DataField]
    public float AttackSpeedModifier = 2.4f;

    [DataField]
    public SoundSpecifier? StartSound = new SoundPathSpecifier("/Audio/ADT/Misc/sande_start.ogg");

    [DataField]
    public SoundSpecifier? EndSound = new SoundPathSpecifier("/Audio/ADT/Misc/sande_end.ogg");

    [DataField] // So it fits the audio
    public TimeSpan ShiftDelay = TimeSpan.FromSeconds(1.9);

    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? RunningSound;

    [ViewVariables(VVAccess.ReadOnly)]
    public SandevistanVisionComponent? Overlay;

    [ViewVariables(VVAccess.ReadOnly)]
    public TrailComponent? Trail;

    [ViewVariables(VVAccess.ReadWrite)]
    public int ColorAccumulator = 0;

    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan EmpLastPulse = TimeSpan.Zero;

    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan EmpCooldown = TimeSpan.FromSeconds(60f);

    [ViewVariables(VVAccess.ReadWrite)]
    public float EmpOverload = 8f;

    [DataField]
    public DamageSpecifier EmpDamage = new()
    {
        DamageDict = new Dictionary<string, FixedPoint2>
        {
            { "Shock", 40 },
        },
    };
}
