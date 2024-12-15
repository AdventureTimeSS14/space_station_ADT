using Content.Shared.Damage;
using Robust.Shared.GameStates;
using Robust.Shared.Audio;

namespace Content.Shared.Climbing.Components;

/// <summary>
///     Makes entity do damage and stun entities with ClumsyComponent
///     upon DragDrop or Climb interactions.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class BonkableComponent : Component
{
    /// <summary>
    ///     How long to stun players on bonk, in seconds.
    /// </summary>
    [DataField]
    public TimeSpan BonkTime = TimeSpan.FromSeconds(2);

    /// <summary>
    ///     How much damage to apply on bonk.
    /// </summary>
    [DataField]
    public DamageSpecifier? BonkDamage;

    // ADT TWEAK START
    /// <summary>
    /// Chance of bonk triggering if the user is clumsy.
    /// </summary>
    [DataField("bonkClumsyChance")]
    public float BonkClumsyChance = 0.5f;

    /// <summary>
    /// Sound to play when bonking.
    /// </summary>
    /// <seealso cref="Bonk"/>
    [DataField("bonkSound")]
    public SoundSpecifier? BonkSound;

    /// <summary>
    /// How long it takes to bonk.
    /// </summary>
    [DataField("bonkDelay")]
    public float BonkDelay = 1.5f;
    // ADT TWEAK END
}
