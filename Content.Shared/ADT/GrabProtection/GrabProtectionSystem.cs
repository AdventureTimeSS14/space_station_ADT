using Content.Shared.ADT.Grab;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Shared.GrabProtection;

public sealed class GrabProtectionSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GrabProtectionComponent, BeforeHarmfulActionEvent>(OnGrab);
    }

    private void OnGrab(EntityUid uid, GrabProtectionComponent component, BeforeHarmfulActionEvent args)
    {
        if (args.Type == HarmfulActionType.Grab)
            args.Cancel();
    }
}
