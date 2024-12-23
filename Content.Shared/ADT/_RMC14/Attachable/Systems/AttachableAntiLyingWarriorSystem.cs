using Content.Shared._RMC14.Attachable.Components;
using Content.Shared._RMC14.Attachable.Events;
using Content.Shared.ADT.Crawling;

namespace Content.Shared._RMC14.Attachable.Systems;

public sealed class AttachableAntiLyingWarriorSystem : EntitySystem
{

    public override void Initialize()
    {
        SubscribeLocalEvent<AttachableAntiLyingWarriorComponent, AttachableAlteredEvent>(OnAttachableAltered);
    }

    private void OnAttachableAltered(Entity<AttachableAntiLyingWarriorComponent> attachable, ref AttachableAlteredEvent args)
    {
        switch (args.Alteration)
        {
            case AttachableAlteredType.Attached:
                EnsureComp<AntiLyingWarriorComponent>(args.Holder);
                break;

            case AttachableAlteredType.Detached:
                RemCompDeferred<AntiLyingWarriorComponent>(args.Holder);
                break;
        }
    }
}
