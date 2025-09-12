using Robust.Shared.Serialization;
using Content.Shared.Damage;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Damage.Systems;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Content.Shared.ADT.Crawling;
using Content.Shared.IdentityManagement;
using Content.Shared.Coordinates;
using Content.Shared.Hands.Components;
using Content.Shared.StatusEffect;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Content.Shared.Speech.Muting;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.Flash.Components;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Movement.Pulling.Components;
using Robust.Shared.Timing;
using Robust.Shared.Network;
using Content.Shared.Mobs.Components;

namespace Content.Shared.ADT.Combat;

[ImplicitDataDefinitionForInheritors]
public partial interface IComboEffect
{
    void DoEffect(EntityUid user, EntityUid target, IEntityManager entMan);
}

/// <summary>
/// наносит урон цели, damage - урон
/// </summary>
[Serializable, NetSerializable]
public sealed partial class ComboDamageEffect : IComboEffect
{
    [DataField]
    public DamageSpecifier Damage;

    public void DoEffect(EntityUid user, EntityUid target, IEntityManager entMan)
    {
        var damageable = entMan.System<DamageableSystem>();
        damageable.TryChangeDamage(target, Damage);
    }
}

/// <summary>
/// наносит цели урон про стамине. StaminaDamage надо указывать целым числом
/// </summary>
[Serializable, NetSerializable]
public sealed partial class ComboStaminaDamageEffect : IComboEffect
{
    [DataField]
    public int StaminaDamage;

    public void DoEffect(EntityUid user, EntityUid target, IEntityManager entMan)
    {
        var stun = entMan.System<SharedStaminaSystem>();
        stun.TakeStaminaDamage(target, StaminaDamage);
    }
}

/// <summary>
/// спавнит на месте цели или пользователя прототип. SpawnOnUser и SpawnOnTarget отвечат за спавн прототипа на юзере и таргете соответственно
/// </summary>
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
            entMan.SpawnAtPosition(SpawnOnUser, user.ToCoordinates());
    }
}

/// <summary>
/// кидает человека на пол. DropItems отвечает за то, будут ли вещи из рук выпадать true - выпадает, false - не выпадает
/// </summary>
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

/// <summary>
/// добавочный урон по тем, кто лежит на земле. IgnoreResistances отвечает за то, будут ли резисты учитываться при нанесения урона
/// </summary>
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

/// <summary>
/// кидачет человека в стан после комбо. Fall отвечает за падение человека, StunTime - время стана, DropItems - выпадают ли вещи при падении
/// </summary>
[Serializable, NetSerializable]
public sealed partial class ComboStunEffect : IComboEffect
{
    [DataField]
    public bool Fall = true;
    [DataField]
    public int StunTime;
    [DataField]
    public bool DropItems = true;

    public void DoEffect(EntityUid user, EntityUid target, IEntityManager entMan)
    {
        if (!entMan.TryGetComponent<StatusEffectsComponent>(target, out var status))
            return;
        var down = entMan.System<SharedStunSystem>();
        down.TryParalyze(target, TimeSpan.FromSeconds(StunTime), false, status, dropItems: DropItems, down: Fall);
    }
}

/// <summary>
/// вызывает попаут на таргете. LocaleText - текст. Желательно использоваль локаль, а так же есть параметры для локали target и user
/// </summary>
[Serializable, NetSerializable]
public sealed partial class ComboPopupEffect : IComboEffect
{
    [DataField]
    public string LocaleText;

    public void DoEffect(EntityUid user, EntityUid target, IEntityManager entMan)
    {
        var popup = entMan.System<SharedPopupSystem>();
        popup.PopupPredicted(Loc.GetString(LocaleText, ("user", Identity.Entity(user, entMan)), ("target", target)), target, target, PopupType.LargeCaution);
    }
}

/// <summary>
/// выбрасывает что угодно из активной руки таргета
/// </summary>
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

/// <summary>
/// перебрасывает вещи из рук в руки
/// </summary>
[Serializable, NetSerializable]
public sealed partial class ComboHamdsRetakeEffect : IComboEffect
{
    public void DoEffect(EntityUid user, EntityUid target, IEntityManager entMan)
    {
        var hands = entMan.System<SharedHandsSystem>();
        if (!entMan.TryGetComponent<HandsComponent>(target, out var targetHand) || targetHand.ActiveHand == null)
            return;
        if (!entMan.TryGetComponent<HandsComponent>(user, out var userHand) || userHand.ActiveHand == null)
            return;
        if (targetHand.ActiveHand.Container == null || targetHand.ActiveHand.Container.ContainedEntity == null)
            return;
        hands.TryDropIntoContainer(user, target, targetHand.ActiveHand.Container);
    }
}

/// <summary>
/// играет любой звук после комбо. Sound - звук, что очевидно
/// </summary>
[Serializable, NetSerializable]
public sealed partial class ComboAudioEffect : IComboEffect
{
    [DataField]
    public SoundSpecifier? Sound;

    public void DoEffect(EntityUid user, EntityUid target, IEntityManager entMan)
    {
        var audio = entMan.System<SharedAudioSystem>();
        audio.PlayPredicted(Sound, user, user);
    }
}

[Serializable, NetSerializable]
public sealed partial class ComboMuteEffect : IComboEffect
{
    [DataField]
    public int Time;

    public void DoEffect(EntityUid user, EntityUid target, IEntityManager entMan)
    {
        var status = entMan.System<StatusEffectsSystem>();
        status.TryAddStatusEffect<MutedComponent>(target, "Muted", TimeSpan.FromSeconds(Time), false);
    }
}

[Serializable, NetSerializable]
public sealed partial class ComboSlowdownEffect : IComboEffect
{
    [DataField]
    public int Time;

    public void DoEffect(EntityUid user, EntityUid target, IEntityManager entMan)
    {
        var status = entMan.System<StatusEffectsSystem>();
        status.TryAddStatusEffect<SlowedDownComponent>(target, "SlowedDown", TimeSpan.FromSeconds(Time), false);
    }
}

/// <summary>
/// добавочный урон по тем, кто лежит на земле. IgnoreResistances отвечает за то, будут ли резисты учитываться при нанесения урона
/// </summary>
[Serializable, NetSerializable]
public sealed partial class ComboMoreStaminaDamageToDownedEffect : IComboEffect
{
    [DataField]
    public float Damage;
    public void DoEffect(EntityUid user, EntityUid target, IEntityManager entMan)
    {
        var down = entMan.System<StandingStateSystem>();
        var stun = entMan.System<SharedStaminaSystem>();

        if (down.IsDown(target))
        {
            stun.TakeStaminaDamage(target, Damage);
        }
    }
}

[Serializable]
public sealed partial class ComboFlashEffect : IComboEffect
{
    [DataField]
    public float Duration;
    [DataField]
    public float SlowDown;
    public void DoEffect(EntityUid user, EntityUid target, IEntityManager entMan)
    {
        var status = entMan.System<StatusEffectsSystem>();
        var blind = entMan.System<BlindableSystem>();

        status.TryAddStatusEffect<FlashedComponent>(target, "Flashed", TimeSpan.FromSeconds(Duration), true);

        blind.AdjustEyeDamage(target, 1);
    }
}

[Serializable]
public sealed partial class ComboStopGrabEffect : IComboEffect
{
    public void DoEffect(EntityUid user, EntityUid target, IEntityManager entMan)
    {
        var pull = entMan.System<PullingSystem>();
        if (!entMan.TryGetComponent<PullerComponent>(user, out var puller) || !entMan.TryGetComponent<PullableComponent>(target, out var pulled))
            return;
        for (int i = (int)puller.Stage; i > 0; i--)
        {
            pull.TryLowerGrabStageOrStopPulling((user, puller), (target, pulled));
        }
        pull.TryStopPull(target, pulled, user);
    }
}

[Serializable]
public sealed partial class ComboStopTargetGrabEffect : IComboEffect
{
    public void DoEffect(EntityUid user, EntityUid target, IEntityManager entMan)
    {
        var pull = entMan.System<PullingSystem>();
        if (!entMan.TryGetComponent<PullerComponent>(target, out var puller) || !entMan.TryGetComponent<PullableComponent>(user, out var pulled))
            return;
        for (int i = (int)puller.Stage; i > 0; i--)
        {
            pull.TryLowerGrabStageOrStopPulling((target, puller), (user, pulled));
        }
        pull.TryStopPull(user, pulled, target);
    }
}

[Serializable]
public sealed partial class ComboTrowTargetEffect : IComboEffect
{
    [DataField]
    public float ThrownSpeed = 7f;
    public void DoEffect(EntityUid user, EntityUid target, IEntityManager entMan)
    {
        var pull = entMan.System<PullingSystem>();
        var transform = entMan.System<SharedTransformSystem>();
        var mapPos = transform.GetMapCoordinates(user).Position;
        var hitPos = transform.GetMapCoordinates(target).Position;
        var dir = hitPos - mapPos;
        pull.Throw(target, user, dir, ThrownSpeed);
    }
}

[Serializable]
public sealed partial class ComboTrowOnUserEffect : IComboEffect
{
    [DataField]
    public float ThrownSpeed = 7f;
    public void DoEffect(EntityUid user, EntityUid target, IEntityManager entMan)
    {
        var pull = entMan.System<PullingSystem>();
        var transform = entMan.System<SharedTransformSystem>();
        var mapPos = transform.GetMapCoordinates(user).Position;
        var hitPos = transform.GetMapCoordinates(target).Position;
        var dir = mapPos - hitPos;
        pull.Throw(target, user, dir, ThrownSpeed);
    }
}

[Serializable]
public sealed partial class ComboEffectToDowned : IComboEffect
{
    [DataField]
    public List<IComboEffect> ComboEvents = new List<IComboEffect> { };
    public void DoEffect(EntityUid user, EntityUid target, IEntityManager entMan)
    {
        var down = entMan.System<StandingStateSystem>();
        if (down.IsDown(target))
        {
            foreach (var comboEvent in ComboEvents)
            {
                comboEvent.DoEffect(user, target, entMan);
            }
        }
    }
}
[Serializable]
public sealed partial class ComboEffectToStanding : IComboEffect
{
    [DataField]
    public List<IComboEffect> ComboEvents = new List<IComboEffect> { };
    public void DoEffect(EntityUid user, EntityUid target, IEntityManager entMan)
    {
        var down = entMan.System<StandingStateSystem>();
        if (!down.IsDown(target))
        {
            foreach (var comboEvent in ComboEvents)
            {
                comboEvent.DoEffect(user, target, entMan);
            }
        }
    }
}
[Serializable]
public sealed partial class ComboEffectToUserPuller : IComboEffect
{
    [DataField]
    public List<IComboEffect> ComboEvents = new List<IComboEffect> { };
    public void DoEffect(EntityUid user, EntityUid target, IEntityManager entMan)
    {
        if (!entMan.TryGetComponent<PullerComponent>(target, out var puller) || puller.Pulling == null || puller.Pulling != user)
            return;
        foreach (var comboEvent in ComboEvents)
        {
            comboEvent.DoEffect(user, target, entMan);
        }
    }
}
[Serializable]
public sealed partial class ComboEffectToPulled : IComboEffect
{
    [DataField]
    public List<IComboEffect> ComboEvents = new List<IComboEffect> { };
    public void DoEffect(EntityUid user, EntityUid target, IEntityManager entMan)
    {
        if (!entMan.TryGetComponent<PullerComponent>(user, out var puller) || puller.Pulling == null || puller.Pulling != target)
            return;
        foreach (var comboEvent in ComboEvents)
        {
            comboEvent.DoEffect(user, target, entMan);
        }
    }
}
[Serializable]
public sealed partial class ComboEffectToStuned : IComboEffect
{
    [DataField]
    public List<IComboEffect> ComboEvents = new List<IComboEffect> { };
    public void DoEffect(EntityUid user, EntityUid target, IEntityManager entMan)
    {
        if (!entMan.HasComponent<StunnedComponent>(target))
            return;
        foreach (var comboEvent in ComboEvents)
        {
            comboEvent.DoEffect(user, target, entMan);
        }
    }
}

/// <summary>
/// После выполнения комбо телепортирует нападающего на цель.
/// </summary>
public sealed partial class ComboEffectTeleportOnVictim : IComboEffect
{
    public void DoEffect(EntityUid user, EntityUid target, IEntityManager entMan)
    {
        var transform = entMan.System<SharedTransformSystem>();

        if (entMan.HasComponent<MobStateComponent>(target))
        {
            transform.SetCoordinates(user, transform.GetMoverCoordinates(target));
        }
    }
}

/// <summary>
/// Меняет местами нападающего и цель.
/// </summary>
public sealed partial class ComboEffectSwapPostion : IComboEffect
{
    public void DoEffect(EntityUid user, EntityUid target, IEntityManager entMan)
    {
        var transform = entMan.System<SharedTransformSystem>();

        if (entMan.HasComponent<MobStateComponent>(target))
        {
            transform.SwapPositions(user, target);
        }
    }
}

