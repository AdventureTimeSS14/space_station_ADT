using Content.Server.Atmos.Components;

namespace Content.Server.Atmos.EntitySystems
{
    public sealed partial class BarotraumaSystem
    {
        /// <summary>
        ///     Пересчитать кэш сопротивления давлению у носителя.
        ///     Нужно тем, кто меняет защиту не через надевание: МОД собирается из частей,
        ///     и перчатки с ботинками не входят в ProtectionSlots, поэтому их развёртывание
        ///     само по себе пересчёт не запускает.
        /// </summary>
        public void RefreshPressureResistance(EntityUid uid)
        {
            if (TryComp<BarotraumaComponent>(uid, out var barotrauma))
                UpdateCachedResistances(uid, barotrauma);
        }
    }
}
