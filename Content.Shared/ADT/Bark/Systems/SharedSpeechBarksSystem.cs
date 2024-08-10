using Robust.Shared.Random;

namespace Content.Shared.ADT.SpeechBarks;

public abstract class SharedSpeechBarksSystem : EntitySystem
{
    [Dependency] protected readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();
    }
}
