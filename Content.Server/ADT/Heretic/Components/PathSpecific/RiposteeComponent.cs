namespace Content.Server.Heretic.Components.PathSpecific;

[RegisterComponent]
public sealed partial class RiposteeComponent : Component
{
    [DataField] public float Cooldown = 5f;
    [ViewVariables(VVAccess.ReadWrite)] public float Timer = 5f;

    [DataField] public bool CanRiposte = true;

    [ViewVariables(VVAccess.ReadWrite)] public bool PendingRiposte;
}
