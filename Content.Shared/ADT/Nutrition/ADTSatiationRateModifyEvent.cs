using Content.Shared.Nutrition.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Nutrition;

[ByRefEvent]
public record struct ADTSatiationRateModifyEvent(ProtoId<SatiationTypePrototype> Type, float Multiplier = 1f);
