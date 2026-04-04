using Content.Shared.Atmos;
using Content.Shared.Damage.Systems;
using Content.Shared.Damage.Components;
using Content.Shared.Damage;
using Content.Server.NodeContainer.Nodes;
using Content.Server.NodeContainer.NodeGroups;
using Content.Shared.ADT.Atmos.EntityDamage.Components;
using Robust.Shared.Random;
using Robust.Shared.GameObjects;

namespace Content.Server.ADT.Atmos.EntityDamage.Systems
{
    /// <summary>
    /// Система которая отвечает за ломание труб при избыточном давлении внутри.
    /// </summary>
    public sealed class OverpressurePipeDamageSystem : EntitySystem
    {
        [Dependency] private readonly DamageableSystem _damage = default!;
        [Dependency] private readonly IRobustRandom _random = default!;

        public void Update(IPipeNet pipeNet)
        {
            foreach (var node in pipeNet.Nodes)
            {
                if (node is PipeNode pipe)
                    HandleOverpressure(pipe, pipeNet.Air);
            }
        }

        public void HandleOverpressure(PipeNode pipe, GasMixture netAir)
        {
            // Проверяем наличие давления в трубе.
            if (netAir == null)
                return;

            if (!EntityManager.TryGetComponent(pipe.Owner, out OverpressurePipeDamageComponent? comp) || comp == null)
                return;

            if (comp.LimitPressure <= 0)
                return;

            float pressure = netAir.Pressure;
            float limit = comp.LimitPressure;
            float over = pressure - limit;

            if (over <= 0)
                return;

            // Наносим урон с случайным шансом, чтобы труба не ломалась моментально.
            float chance = 0.5f;

            if (EntityManager.TryGetComponent(pipe.Owner, out DamageableComponent? dmg) && dmg != null)
            {
                float totalDamage = (float)dmg.TotalDamage;
                chance = Math.Clamp(0.5f + totalDamage * 0.5f, 0f, 1f);
            }

            if (_random.Prob(1 - chance))
                return;

            // Чем больше давление - тем больше урона.
            int dmgAmt = (int)(10f * MathF.Exp(over / limit));
            if (dmgAmt <= 0)
                return;

            var spec = new DamageSpecifier();
            spec.DamageDict["Structural"] = dmgAmt;

            _damage.TryChangeDamage(pipe.Owner, spec);
        }
    }
}