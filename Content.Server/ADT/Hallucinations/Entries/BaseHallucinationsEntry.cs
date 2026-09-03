using Content.Shared.Destructible.Thresholds;
using Robust.Shared.Random;

namespace Content.Server.ADT.Hallucinations.Entries;

/// <summary>
/// Base class for different hallucination types
/// </summary>
public abstract class BaseHallucinationsEntry
{
    /// <summary>
    /// Performing delay
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public MinMax Delay = new();

    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan NextPerform = TimeSpan.Zero;

    /// <summary>
    /// Tries to perform hallucination effect
    /// </summary>
    /// <param name="source">Hallucinating entity</param>
    /// <param name="curTime">Current time to check timer</param>
    public void TryPerform(EntityUid source, IEntityManager entMan, IRobustRandom random, TimeSpan curTime)
    {
        if (curTime < NextPerform)
            return;

        NextPerform = curTime + TimeSpan.FromSeconds(Delay.Next(new Random()));
        Perform(source, entMan, random);
    }

    protected abstract void Perform(EntityUid source, IEntityManager entMan, IRobustRandom random);
}
