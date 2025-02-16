using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Content.Shared.Damage;
using Content.Shared.Hands.EntitySystems;
using Robust.Shared.Audio;
using Content.Shared.Popups;
using System.Numerics;
using Content.Shared.Damage.Systems;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Content.Shared.ADT.Crawling;
using Content.Shared.IdentityManagement;
using Content.Shared.Coordinates;
using Content.Shared.Throwing;
using Content.Shared.Hands.Components;

namespace Content.Shared.ADT.Combat;

[RegisterComponent, NetworkedComponent]
public sealed partial class ComboComponent : Component
{
    [DataField]
    public List<CombatMove> AvailableMoves { get; private set; } = new List<CombatMove>();
    public EntityUid TargetEntity;
    public List<CombatAction> CurrestActions { get; private set; } = new List<CombatAction>();
}

[Serializable, NetSerializable]
public enum CombatAction
{
    Disarm,
    Grab,
    Hit
}

[DataDefinition]
public sealed partial class CombatMove
{
    /// <summary>
    /// это надо указывать в прототипе в виде списка
    /// </summary>
    [DataField]
    public List<CombatAction> ActionsNeeds { get; private set; } = new List<CombatAction>();

    [ViewVariables(VVAccess.ReadWrite)]
    public DamageSpecifier ComboDamage = default!;

    [DataField]
    public List<IComboEffect> ComboEvent = new List<IComboEffect>{};
}

[ImplicitDataDefinitionForInheritors]
public partial interface IComboEffect
{
    void DoEffect(EntityUid user, EntityUid target, IEntityManager entMan);
}

[Serializable, NetSerializable]
public sealed partial class ComboDamageEffect : IComboEffect
{
    [DataField(required: true)]
    public DamageSpecifier Damage;

    public void DoEffect(EntityUid user, EntityUid target, IEntityManager entMan)
    {
        var damageable = entMan.System<DamageableSystem>();
        damageable.TryChangeDamage(target, Damage);
    }
}
[Serializable, NetSerializable]
public sealed partial class ComboStaminaDamageEffect : IComboEffect
{
    [DataField]
    public int StaminaDamage;

    public void DoEffect(EntityUid user, EntityUid target, IEntityManager entMan)
    {
        var stun = entMan.System<StaminaSystem>();
        stun.TakeStaminaDamage(target, StaminaDamage);
    }
}
[Serializable, NetSerializable]
public sealed partial class ComboTrowEffect : IComboEffect
{
    [DataField]
    public int Tiles;
    [DataField]
    public int TrowPower = 8;

    public void DoEffect(EntityUid user, EntityUid target, IEntityManager entMan)
    {
        var userCoordinates = user.ToCoordinates();

        var targetCoordinates = target.ToCoordinates();

        var direction = (targetCoordinates.Position - userCoordinates.Position).Normalized();

        var targetTileX = userCoordinates.X + direction.X * 2;
        var targetTileY = userCoordinates.Y + direction.Y * 2;

        int tileX = (int)MathF.Round(targetTileX);
        int tileY = (int)MathF.Round(targetTileY);

        var targetTile = new Vector2(tileX, tileY);
        var trow = entMan.System<ThrowingSystem>();
        trow.TryThrow(target, targetTile, TrowPower, animated: false, playSound: false, doSpin: false);
    }
}
[Serializable, NetSerializable]
public sealed partial class ComboSpawnEffect : IComboEffect
{
    [DataField]
    public string? SpawnOnUser;
    [DataField]
    public string? SpawnOnTarget;

    public void DoEffect(EntityUid user, EntityUid target, IEntityManager entMan)
    {
        if (SpawnOnTarget != null)
            entMan.SpawnAtPosition(SpawnOnTarget, target.ToCoordinates());
        if (SpawnOnUser != null)
            entMan.SpawnAtPosition(SpawnOnUser, target.ToCoordinates());
    }
}
[Serializable, NetSerializable]
public sealed partial class ComboFallEffect : IComboEffect
{
    [DataField]
    public bool DropItems;

    public void DoEffect(EntityUid user, EntityUid target, IEntityManager entMan)
    {
        if (!entMan.HasComponent<CrawlerComponent>(target))
            return;
        var down = entMan.System<StandingStateSystem>();
        down.Down(target, dropHeldItems: DropItems);
    }
}

[Serializable, NetSerializable]
public sealed partial class ComboMoreDamageToDownedEffect : IComboEffect
{
    [DataField(required: true)]
    public DamageSpecifier Damage;
    [DataField]
    public bool IgnoreResistances;
    public void DoEffect(EntityUid user, EntityUid target, IEntityManager entMan)
    {
        var down = entMan.System<StandingStateSystem>();
        var damageable = entMan.System<DamageableSystem>();
        if (down.IsDown(target))
        {
            damageable.TryChangeDamage(target, Damage, IgnoreResistances);
        }
    }
}
[Serializable, NetSerializable]
public sealed partial class ComboStunEffect : IComboEffect
{
    [DataField]
    public bool Fall = true;
    [DataField]
    public TimeSpan StunTime;
    [DataField]
    public bool DropItems = true;

    public void DoEffect(EntityUid user, EntityUid target, IEntityManager entMan)
    {
        var down = entMan.System<SharedStunSystem>();
        down.TryKnockdown(target, StunTime, true, dropItems: DropItems, down: Fall);
    }
}
[Serializable, NetSerializable]
public sealed partial class ComboDropFromActiveHandEffect : IComboEffect
{
    [DataField]
    public bool Fall = true;
    [DataField]
    public TimeSpan StunTime;
    [DataField]
    public bool DropItems = true;

    public void DoEffect(EntityUid user, EntityUid target, IEntityManager entMan)
    {
        var down = entMan.System<SharedStunSystem>();
        down.TryKnockdown(target, StunTime, true, dropItems: DropItems, down: Fall);
    }
}
[Serializable, NetSerializable]
public sealed partial class ComboPopupEffect : IComboEffect
{
    [DataField]
    public string LocaleText;

    public void DoEffect(EntityUid user, EntityUid target, IEntityManager entMan)
    {
        var popup = entMan.System<SharedPopupSystem>();
        popup.PopupEntity(Loc.GetString(LocaleText, ("user", Identity.Entity(user, entMan)), ("target", target)), target, PopupType.LargeCaution);
    }
}
[Serializable, NetSerializable]
public sealed partial class ComboDropFromHandsEffect : IComboEffect
{
    public void DoEffect(EntityUid user, EntityUid target, IEntityManager entMan)
    {
        var hands = entMan.System<SharedHandsSystem>();
        if (!entMan.TryGetComponent<HandsComponent>(target, out var hand) || hand.ActiveHand == null)
            return;
        hands.DoDrop(target, hand.ActiveHand);
    }
}
