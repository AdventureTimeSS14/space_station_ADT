using Content.Shared.Climbing.Systems;
using Content.Shared.Clumsy.Components;
using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Climbing.Components;

/// <summary>
/// Damages and stuns entities afflicted with <see cref="ClumsyVaultStatusEffectComponent"/>  upon climb interactions.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(ClimbSystem))]
public sealed partial class BonkableComponent : Component
{
    /// <summary>
    /// How long to stun players on bonk, in seconds.
    /// </summary>
    [DataField]
    public TimeSpan BonkTime = TimeSpan.FromSeconds(2);

    // ADT TWEAK START

    /// <summary>
    ///     Chance of bonk triggering if the user is clumsy.
    /// </summary>

    [DataField("bonkClumsyChance")]
    public float BonkClumsyChance = 0.5f;

    /// <summary>
    /// How long it takes to bonk.
    /// </summary>
    [DataField("bonkDelay")]
    public float BonkDelay = 1.5f;
    // ADT TWEAK END

    [DataField]
    public DamageSpecifier? BonkDamage;

    /// <summary>
    /// The sound of the bonk.
    /// </summary>
    [DataField]
    public SoundSpecifier BonkSound = new SoundCollectionSpecifier("TrayHit");
}
