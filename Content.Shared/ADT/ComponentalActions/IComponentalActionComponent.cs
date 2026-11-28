using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.ComponentalActions;

public interface IComponentalActionComponent
{
    EntProtoId Action { get; }

    EntityUid? ActionEntity { get; set; }
}
