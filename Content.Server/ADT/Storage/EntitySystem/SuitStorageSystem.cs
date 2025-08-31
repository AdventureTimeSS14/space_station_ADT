using Content.Shared.ADT.Storage.Components;
using Content.Shared.Verbs;
using Content.Shared.UserInterface;
using Robust.Shared.Player;

namespace Content.Shared.ADT.Storage.System;

public sealed class SuitStorageSystem : EntitySystem
{
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SuitStorageComponent, GetVerbsEvent<Verb>>(OnGetVerbs);
        SubscribeLocalEvent<SuitStorageComponent, SuitStorageMessage>(OnSelect);
    }

    private void OnGetVerbs(Entity<SuitStorageComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        if (!HasComp<ActorComponent>(args.User))
            return;

        var user = args.User;
        var owner = ent.Owner;

        var verb = new Verb
        {
            Text = Loc.GetString("alt-select-open-ui"),
            Act = () =>
            {
                _ui.TryOpenUi(owner, SuitStorageUiKey.Key, user);
            }
        };

        args.Verbs.Add(verb);
    }

    private void OnSelect(Entity<SuitStorageComponent> ent, ref SuitStorageMessage args)
    {
        if (args.Index < 0 || args.Index >= ent.Comp.Options.Count)
            return;

        ent.Comp.Selected = args.Index;
        Dirty(ent);

        Log.Info($"[SuitStorage] {ToPrettyString(ent)} выбрал {ent.Comp.Options[args.Index]}");
    }
}