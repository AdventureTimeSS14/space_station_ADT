namespace Content.Server.ADT.Emp.Components;

[RegisterComponent]
public sealed partial class EmpSpeechComponent : Component
{
    [DataField]
    public float DurationMultiplier = 1f;

    [DataField]
    public float MaxDuration = 15f;

    [DataField]
    public int AccentsAmount = 3;

    public static HashSet<string> Accents = new()
    {
        "StatusEffectStutter",
        "StatusEffectScrambled",
        "StatusEffectOwO",
        "StatusEffectAllCaps",
        "StatusEffectBark"
    };
}
