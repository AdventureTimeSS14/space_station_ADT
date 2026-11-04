using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Surgery.Events;

[Serializable, NetSerializable]
public sealed partial class SurgeryStepDoAfterEvent : DoAfterEvent
{
    public string EdgeId;
    public int StepIndex;

    public SurgeryStepDoAfterEvent(string edgeId, int stepIndex)
    {
        EdgeId = edgeId;
        StepIndex = stepIndex;
    }

    public override DoAfterEvent Clone() => this;
}
