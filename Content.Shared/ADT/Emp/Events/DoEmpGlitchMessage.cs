using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Emp;

[Serializable, NetSerializable]
public sealed partial class DoEmpGlitchMessage : EntityEventArgs
{
    public float Duration;
    public float MotionStrength;
    public float Smear;
    public float Glitch;

    public DoEmpGlitchMessage(float duration, float motionStr, float smear, float glitch)
    {
        Duration = duration;
        MotionStrength = motionStr;
        Smear = smear;
        Glitch = glitch;
    }
}
