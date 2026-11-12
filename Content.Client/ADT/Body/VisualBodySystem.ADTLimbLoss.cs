using Content.Client.ADT.Body;
using Content.Shared.Body;

namespace Content.Client.Body;

public sealed partial class VisualBodySystem
{
    [Dependency] private LimbLossVisualsSystem _limbLossVisuals = default!;

    public void RefreshMarkings(EntityUid body)
    {
        if (!TryComp<BodyComponent>(body, out var bodyComp) || bodyComp.Organs is not { } organs)
            return;

        foreach (var organ in organs.ContainedEntities)
        {
            if (!TryComp<VisualOrganMarkingsComponent>(organ, out var markings))
                continue;

            RemoveMarkings((organ, markings), body);
            ApplyMarkings((organ, markings), body);
        }
    }
}
