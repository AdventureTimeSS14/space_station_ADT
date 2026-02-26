using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.ADT.Predictor;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PredictorMachineComponent : Component
{
    [DataField("price")]
    [AutoNetworkedField]
    public int Price = 5;

    [DataField("normalPredictionsPack", customTypeSerializer: typeof(PrototypeIdSerializer<PredictorPackPrototype>))]
    public string? NormalPredictionsPack;

    [DataField("emaggedPredictionsPack", customTypeSerializer: typeof(PrototypeIdSerializer<PredictorPackPrototype>))]
    public string? EmaggedPredictionsPack;

    [DataField("specialPredictionChance")]
    public float SpecialPredictionChance = 0.05f;

    [DataField("animationState")]
    public string AnimationState = "on-predicts";

    [DataField("onState")]
    public string OnState = "on";

    [DataField("offState")]
    public string OffState = "off";

    [DataField("emaggedState")]
    public string EmaggedState = "emagged";

    [DataField("emaggedAnimationState")]
    public string EmaggedAnimationState = "emagged-on-predicts";

    [DataField("animationCycles")]
    public int AnimationCycles = 2;

    [DataField("soundPayment")]
    public SoundSpecifier? SoundPayment;

    [DataField("soundDeny")]
    public SoundSpecifier? SoundDeny;

    [AutoNetworkedField]
    public bool IsAnimating = false;
}

[Serializable, NetSerializable]
public enum PredictorMachineVisuals : byte
{
    State
}

[Serializable, NetSerializable]
public enum PredictorMachineState : byte
{
    Off,
    On,
    Predicting,
    EmaggedOn,
    EmaggedPredicting
}
