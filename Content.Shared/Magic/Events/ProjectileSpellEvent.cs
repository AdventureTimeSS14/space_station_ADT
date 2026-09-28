using Content.Shared.Actions;
using Robust.Shared.Prototypes;

namespace Content.Shared.Magic.Events;

public sealed partial class ProjectileSpellEvent : WorldTargetActionEvent
{
    /// <summary>
    /// What entity should be spawned.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId Prototype;
<<<<<<< ours

    // ADT-Heretic: скорость снаряда настраивается (дефолт 25f — прежнее поведение ADT)
    [DataField]
    public float Speed = 25f;
||||||| base
=======

    /// <summary>
    /// How fast the projectile should travel
    /// </summary>
    [DataField]
    public float ProjectileSpeed = 25f;
>>>>>>> theirs
}
