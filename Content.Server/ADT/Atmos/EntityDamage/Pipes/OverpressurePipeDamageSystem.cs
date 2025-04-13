using Content.Shared.Atmos;
using Content.Shared.Damage;
using Content.Server.NodeContainer.Nodes;
using Content.Shared.ADT.Atmos.EntityDamage.Pipes;
using Robust.Shared.Random;
using Robust.Shared.GameObjects;

namespace Content.Server.ADT.Atmos.EntityDamage.Pipes
{
    public sealed class OverpressurePipeDamageSystem : EntitySystem
    {
        [Dependency] private readonly DamageableSystem _damage = default!;
        [Dependency] private readonly IRobustRandom _random = default!;

        public void HandleOverpressure(PipeNode pipe, GasMixture netAir)
        {
            // Checks pressure in pipe.
            if (netAir == null ||
                !EntityManager.TryGetComponent(pipe.Owner, out OverpressurePipeDamageComponent? comp) ||
                comp.LimitPressure <= 0)
                return;

            float pressure = netAir.Pressure;
            float limit = comp.LimitPressure;

            float over = pressure - limit;
            if (over <= 0) return;

            // If the pressure is bigger than our limit pressure (check component), then with a 50% chance we get damage
            float chance = EntityManager.TryGetComponent(pipe.Owner, out DamageableComponent? dmg)
                ? Math.Clamp(0.5f + (float)dmg.TotalDamage * 0.5f, 0f, 1f)
                : 0.5f;

            if (_random.Prob(1 - chance)) return;

            int dmgAmt = (int)(10f * MathF.Exp(over / limit));
            if (dmgAmt <= 0) return;

            _damage.TryChangeDamage(pipe.Owner, new DamageSpecifier { DamageDict = { ["Structural"] = dmgAmt } });
        }
    }
}