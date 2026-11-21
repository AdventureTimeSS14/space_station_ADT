namespace Content.Server.ADT.Mech.Components;


[RegisterComponent]
public sealed partial class MechEmpGlitchComponent : Component
{
    #region Glitch config
    [DataField]
    public float CameraGlitchDurationMultiplier = .25f;

    [DataField]
    public float CameraGlitchMotionStrength = 12f;

    [DataField]
    public float CameraGlitchSmear = 0.6f;

    [DataField]
    public float CameraGlitchAmount = 0.5f;

    [ViewVariables]
    public bool HadCameraGlitch = false;

    [ViewVariables]
    public float CachedCameraGlitchDurationMultiplier = 1f;

    [ViewVariables]
    public float CachedCameraGlitchMotionStrength = 6f;

    [ViewVariables]
    public float CachedCameraGlitchSmear = 0.6f;

    [ViewVariables]
    public float CachedCameraGlitchAmount = 0.5f;
    #endregion

}
