using Robust.Shared.Timing;

namespace Content.Server.ADT.StationAi;

[RegisterComponent]
public sealed partial class AiEyeTeleportComponent : Component
{
    [DataField]
    public TimeSpan NextTeleportAt;

    [DataField]
    public float Cooldown = 10f;
}