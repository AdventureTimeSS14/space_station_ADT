using Content.Shared.Mind.Components;

namespace Content.Shared.Mind;

public abstract partial class SharedMindSystem
{
    public void ClearLastMind(EntityUid uid, EntityUid mindId)
    {
        if (!TryComp<MindContainerComponent>(uid, out var container) || container.LastMind != mindId)
            return;

        container.LastMind = null;
        Dirty(uid, container);
    }
}
