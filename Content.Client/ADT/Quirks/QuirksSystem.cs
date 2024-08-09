using Content.Shared.ADT.Traits;
using Content.Client.Storage.Components;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Verbs;
using Content.Shared.Tools.Components;
using Content.Shared.Whitelist;
using Content.Shared.Lock;

namespace Content.Client.ADT.Traits;

public sealed class QuirksSystem : SharedQuirksSystem
{
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EntityStorageComponent, GetVerbsEvent<AlternativeVerb>>(OnGetHideVerbs);

    }

    private void OnGetHideVerbs(EntityUid uid, EntityStorageComponent comp, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        if (!HasComp<FastLockersComponent>(args.User))
            return;
        if (TryComp<WeldableComponent>(uid, out var weldable) && weldable.IsWelded)
            return;
        if (_whitelist.IsWhitelistFail(comp.Whitelist, args.User))
            return;
        if (TryComp<LockComponent>(uid, out var lockComponent) && lockComponent.Locked)
            return;

        AlternativeVerb verb = new()
        {
            Act = () => TryHide(args.User, uid),
            Text = Loc.GetString("quirk-fast-locker-hide-verb"),
        };
        args.Verbs.Add(verb);

    }

}
