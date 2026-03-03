using Content.Shared.ADT.PunchingBag;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Player;

namespace Content.Server.ADT.PunchingBag;

public sealed class PunchingBagAnimationsSystem : SharedPunchingBagAnimationsSystem
{
    // Отслеживание прогресса силы для каждого игрока
    private readonly Dictionary<EntityUid, float> _playerPullStrength = new();

    protected override void PlayAnimation(EntityUid uid, EntityUid attacker, string animationState)
    {
        var filter = Filter.Pvs(uid, entityManager: EntityManager);

        if (TryComp(attacker, out ActorComponent? actor))
            filter.RemovePlayer(actor.PlayerSession);

        RaiseNetworkEvent(new PunchingBagAnimationEvent(GetNetEntity(uid), animationState), filter);

        PullerComponent? pullerComp = null;
        if (Resolve(attacker, ref pullerComp))
        {
            // Увеличиваем прогресс силы на 0.02 при каждом ударе
            if (!_playerPullStrength.TryGetValue(attacker, out var currentStrength))
            {
                currentStrength = 0f;
            }

            currentStrength += 0.02f;
            _playerPullStrength[attacker] = currentStrength;

            // Устанавливаем уровень PulledDensityReduction на основе накопленной силы
            float targetReduction;
            if (currentStrength >= 1.0f)
            {
                targetReduction = 1.0f; // Последний уровень (максимум 90%)
            }
            else if (currentStrength >= 0.70f)
            {
                targetReduction = 0.70f; // Второй уровень
            }
            else if (currentStrength >= 0.40f)
            {
                targetReduction = 0.40f; // Первый уровень
            }
            else
            {
                targetReduction = pullerComp.PulledDensityReduction; // Оставляем текущее значение
            }

            pullerComp.PulledDensityReduction = targetReduction;
            Dirty(attacker, pullerComp);
        }
    }
}

