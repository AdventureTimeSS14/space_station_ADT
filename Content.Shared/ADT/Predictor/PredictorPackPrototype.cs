using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Predictor;

[Prototype]
public sealed partial class PredictorPackPrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField("predictions", required: true)]
    public List<string> Predictions { get; private set; } = new();
}
