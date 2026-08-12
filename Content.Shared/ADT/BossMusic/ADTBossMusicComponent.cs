using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.BossMusic;

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class ADTBossMusicComponent : Component
{
    [DataField]
    public ProtoId<ADTBossMusicPrototype>? Music;

    [DataField]
    public float Range = 18f;

    [DataField]
    public float ExitRange = 26f;

    [DataField]
    public TimeSpan ExitDelay = TimeSpan.FromSeconds(5);

    [DataField]
    public TimeSpan CombatTimeout = TimeSpan.FromSeconds(15);

    [DataField, AutoPausedField]
    public TimeSpan CombatUntil;

    [ViewVariables]
    public Dictionary<EntityUid, TimeSpan?> Listeners = new();
}
