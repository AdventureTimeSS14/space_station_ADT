using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Hierophant.Effects;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause]
public sealed partial class HierophantBlastComponent : Component
{
    [DataField]
    public EntityUid? Caster;

    [DataField]
    public float Damage = 10f;

    [DataField]
    public DamageSpecifier DamageTypes = new()
    {
        DamageDict =
        {
            { "Heat", 1 },
        },
    };

    [DataField]
    public bool MonsterDamageBoost = true;

    [DataField]
    public float MonsterDamageMultiplier = 2f;

    [DataField]
    public bool FriendlyFireCheck;

    [DataField]
    public TimeSpan DetonateDelay = TimeSpan.FromSeconds(0.6);

    [DataField]
    public TimeSpan BurstDuration = TimeSpan.FromSeconds(0.13);

    [DataField, AutoPausedField]
    public TimeSpan DetonateAt;

    [DataField, AutoPausedField]
    public TimeSpan BurstEndAt;

    [DataField]
    public bool Bursting;

    [DataField]
    public HashSet<EntityUid> HitThings = new();

    [DataField]
    public SoundSpecifier SpawnSound = new SoundPathSpecifier("/Audio/ADT/Hierophant/blind.ogg");

    [DataField]
    public SoundSpecifier HitSound = new SoundPathSpecifier("/Audio/ADT/Hierophant/sear.ogg");

    [DataField]
    public Color FlashColor = Color.FromHex("#660099");
}
