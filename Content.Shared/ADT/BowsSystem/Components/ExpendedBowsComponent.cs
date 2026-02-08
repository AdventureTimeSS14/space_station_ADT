using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.ADT.BowsSystem.Components;
[RegisterComponent, NetworkedComponent]
public sealed partial class ExpendedBowsComponent : Component
{

    /// <summary>
    /// Sound to bow on tension
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier bowSound = new SoundPathSpecifier("/Audio/Weapons/Guns/Misc/arrow_nock.ogg");

    /// <summary>
    /// Zero state timer
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan coldownStart = TimeSpan.Zero;

    /// <summary>
    /// Time to timer
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float floatToColdown = 7f;

    /// <summary>
    /// Aaa.....Step of tension in bow 
    /// </summary>
    public int StepOfTension=0;

    /// <summary>
    /// Slot in bow for arrow
    /// </summary>
    public string ItemSlot="projectiles";

    /// <summary>
    /// Tension and loc for it
    /// </summary>
    public Dictionary<int, string> TensionAndLoc = new Dictionary<int, string>
    {
        {0, "popup-bow-use-null"},
        {1, "popup-bow-use-light"},
        {2, "popup-bow-use-medium"},
        {3, "popup-bow-use-hard"},
    };

    /// <summary>
    /// Tension in bow and speed modiefer for arrow
    /// </summary>
    public Dictionary<int, float> TensionAndModieferSpeed = new Dictionary<int, float>
    {
        {0, 0.5f},
        {1, 1f},
        {2, 1.4f},
        {3, 3f},
    };

    /// <summary>
    /// Damage that have bonus
    /// </summary>
    public string DamageToModifying = "Piercing";

    /// <summary>
    /// Max tension in bow
    /// </summary>
    public int MaxTension = 3;

    /// <summary>
    /// Min tension in bow
    /// </summary>
    public int MinTension = 0;
}