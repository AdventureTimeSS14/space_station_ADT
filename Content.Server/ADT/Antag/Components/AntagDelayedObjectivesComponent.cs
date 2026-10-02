using Content.Server.ADT.Antag;
using Content.Server.Antag.Components;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Antag.Components;

[RegisterComponent, Access(typeof(AntagDelayedObjectivesSystem))]
public sealed partial class AntagDelayedObjectivesComponent : Component
{
    [DataField(required: true)]
    public List<AntagObjectiveSet> Sets = new();

    [DataField(required: true)]
    public float MaxDifficulty;

    [DataField]
    public TimeSpan Delay = TimeSpan.FromMinutes(30);

    [DataField]
    public TimeSpan? MaxDelay;

    [DataField]
    public SoundSpecifier GreetSoundNotification = new SoundCollectionSpecifier("ADTTraitorStart");
}