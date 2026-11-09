using Content.Shared.ADT.Economy;
using Content.Shared.Cargo.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Economy;

public sealed class StationSalaryCutSystem : EntitySystem
{
    public float SetCut(EntityUid station, ProtoId<CargoAccountPrototype> department, float? cut)
    {
        if (cut is not { } value || value <= 0f)
        {
            if (TryComp<StationSalaryCutComponent>(station, out var existing))
            {
                existing.Cuts.Remove(department);

                if (existing.Cuts.Count == 0)
                    RemComp<StationSalaryCutComponent>(station);
            }

            return 0f;
        }

        var cuts = EnsureComp<StationSalaryCutComponent>(station);
        cuts.Cuts[department] = Math.Clamp(value, 0f, cuts.MaxCut);
        return cuts.Cuts[department];
    }

    public float AddCut(EntityUid station, ProtoId<CargoAccountPrototype> department, float add)
    {
        var cuts = EnsureComp<StationSalaryCutComponent>(station);
        cuts.Cuts[department] = Math.Min(cuts.Cuts.GetValueOrDefault(department) + add, cuts.MaxCut);
        return cuts.Cuts[department];
    }

    public float GetCut(EntityUid station, ProtoId<CargoAccountPrototype> department)
    {
        return TryComp<StationSalaryCutComponent>(station, out var cuts) &&
               cuts.Cuts.TryGetValue(department, out var cut)
            ? cut
            : 0f;
    }
}
