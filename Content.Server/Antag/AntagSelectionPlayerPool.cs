using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server.Antag;

// ADT-Tweak-Start
public sealed class AntagSelectionPlayerPool (List<List<ICommonSession>> orderedPools, Func<ICommonSession, float>? weightSelector = null)
// ADT-Tweak-End
{
    public bool TryPickAndTake(IRobustRandom random, [NotNullWhen(true)] out ICommonSession? session)
    {
        session = null;

        foreach (var pool in orderedPools)
        {
            if (pool.Count == 0)
                continue;

            // ADT-Tweak-Start
            session = weightSelector == null
                ? random.PickAndTake(pool)
                : PickAndTakeWeighted(random, pool, weightSelector);
            // ADT-Tweak-End
            break;
        }

        return session != null;
    }

    // ADT-Tweak-Start
    private static ICommonSession PickAndTakeWeighted(IRobustRandom random, List<ICommonSession> pool, Func<ICommonSession, float> weight)
    {
        var total = 0f;
        foreach (var candidate in pool)
        {
            total += MathF.Max(weight(candidate), 0f);
        }

        if (total <= 0f)
            return random.PickAndTake(pool);

        var roll = random.NextFloat() * total;
        for (var i = 0; i < pool.Count; i++)
        {
            roll -= MathF.Max(weight(pool[i]), 0f);
            if (roll > 0f)
                continue;

            var session = pool[i];
            pool.RemoveAt(i);
            return session;
        }

        return random.PickAndTake(pool);
    }
    // ADT-Tweak-End

    public int Count => orderedPools.Sum(p => p.Count);

    // ADT-Tweak-Start
    public IEnumerable<ICommonSession> AllCandidates => orderedPools.SelectMany(p => p);
    // ADT-Tweak-End
}
