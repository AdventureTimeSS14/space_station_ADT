using Content.Shared.Whitelist; //ADT-Tweak
using Robust.Shared.Physics.Events;

namespace Content.Shared.Physics;

public sealed class SharedPreventCollideSystem : EntitySystem
{
    //ADT-Tweak-Start
    [Dependency] private EntityWhitelistSystem _whitelist = default!;
    //ADT-Tweak-End

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PreventCollideComponent, PreventCollideEvent>(OnPreventCollide);
    }

    private void OnPreventCollide(EntityUid uid, PreventCollideComponent component, ref PreventCollideEvent args)
    {
        if (component.Uid == args.OtherEntity)
            args.Cancelled = true;

        //ADT-Tweak-Start
        if (component.Whitelist != null && _whitelist.IsValid(component.Whitelist, args.OtherEntity))
            args.Cancelled = true;
        //ADT-Tweak-End
    }

}
