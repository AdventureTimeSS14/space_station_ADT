using Content.Server.Atmos.EntitySystems;
using Content.Server.NodeContainer.NodeGroups;
using Content.Shared.ADT.Atmos.Piping.Components;
using Content.Shared.Damage;
using Robust.Shared.Random;

namespace Content.Server.ADT.Atmos.Piping.Systems
{
    public sealed class OverpressurePipeDamageSystem : EntitySystem
    {
        [Dependency] private readonly DamageableSystem _damage = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly AtmosphereSystem _atmosphere = default!;

        private float _accumulator;
        private const float UpdateInterval = 1.0f;
        private const float DamageProbability = 0.5f;

        public override void Update(float frameTime)
        {
            _accumulator += frameTime;
        }

        public void Update(PipeNet net)
        {
            if (_accumulator < UpdateInterval)
                return;

            _accumulator = 0f;

            float pressure = net.Air.Pressure;
            var entities = net.OverpressurePipeEntities;

            // Заменил foreach для всех сущностей в пайпнете на for в списке. В теории, кэширование должно помочь с отпимизацией системы.
            for (var i = 0; i < entities.Count; i++)
            {
                var uid = entities[i];
                
                if (!EntityManager.TryGetComponent(uid, out OverpressurePipeDamageComponent? comp))
                    continue;

                float limitPressure = comp.LimitPressure;
                float over = pressure - limitPressure;
                
                if (over <= 0)
                    continue;

                int dmg = CalculateDamage(over, limitPressure);
                if (dmg <= 0)
                    continue;

                // Чтобы трубу не ломало моментально, проносим урон с случайным шансом.
                if (!_random.Prob(DamageProbability))
                    continue;

                // Урон не наносится, если давление на тайле < MaxTilePressure И давление в трубе < давление на тайле
                // Если атмосферы на тайле нет, проверка пропускается и урон наносится.
                var tileAtmos = _atmosphere.GetContainingMixture(uid, false, true);
                if (tileAtmos != null 
                    && tileAtmos.Pressure < comp.MaxTilePressure 
                    && pressure < tileAtmos.Pressure)
                    continue;

                _damage.TryChangeDamage(
                    uid,
                    new DamageSpecifier
                    {
                        DamageDict = { ["Structural"] = dmg }
                    });
            }
        }

        private static int CalculateDamage(float over, float limit)
        {
            if (limit <= 0 || over <= 0)
                return 0;

            // Убрал вызовы мат.функций, в теории должно лучше работать.
            float ratio = over / limit;
            return (int)(5f + ratio * ratio * 15f);
        }
    }
}