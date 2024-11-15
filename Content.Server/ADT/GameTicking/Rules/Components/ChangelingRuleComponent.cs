using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Content.Shared.Store;

namespace Content.Server.GameTicking.Rules.Components;

[RegisterComponent, Access(typeof(ChangelingRuleSystem))]
public sealed partial class ChangelingRuleComponent : Component
{
    public readonly List<EntityUid> Minds = new();

    /// <summary>
    /// Path to changeling start sound.
    /// </summary>
    [DataField]
    public SoundSpecifier ChangelingStartSound = new SoundPathSpecifier("/Audio/Ambience/Antag/changeling_start.ogg");
}
