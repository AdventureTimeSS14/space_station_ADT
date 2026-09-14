using Content.Shared.ADT.Salvage.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Salvage.Systems;

public sealed class ADTEverfullSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ADTEverfullComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerbs);
        SubscribeLocalEvent<ADTEverfullComponent, ADTEverfullSelectMessage>(OnSelected);
    }

    private void OnGetVerbs(Entity<ADTEverfullComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        var user = args.User;

        args.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString("adt-everfull-verb-fill"),
            Act = () =>
            {
                if (TryReady(ent, user))
                    _ui.TryOpenUi(ent.Owner, ADTEverfullUiKey.Key, user);
            },
        });
    }

    private void OnSelected(Entity<ADTEverfullComponent> ent, ref ADTEverfullSelectMessage args)
    {
        var user = args.Actor;

        if (args.Index < 0 || args.Index >= ent.Comp.Options.Count)
            return;

        if (!TryReady(ent, user))
            return;

        if (!_solution.TryGetSolution(ent.Owner, ent.Comp.Solution, out var soln, out _))
            return;

        var option = ent.Comp.Options[args.Index];

        ent.Comp.NextRefill = _timing.CurTime + ent.Comp.RefillCooldown;
        ent.Comp.Filling = option.Reagent;
        ent.Comp.NextFillTick = _timing.CurTime;

        _solution.RemoveAllSolution(soln.Value);
        _popup.PopupEntity(
            Loc.GetString("adt-everfull-fill",
                ("mug", ent.Owner),
                ("contents", Loc.GetString(option.Description))),
            user,
            user);

        _ui.CloseUi(ent.Owner, ADTEverfullUiKey.Key, user);
    }

    private bool TryReady(Entity<ADTEverfullComponent> ent, EntityUid user)
    {
        if (ent.Comp.Filling != null)
        {
            _popup.PopupEntity(Loc.GetString("adt-everfull-already-filling"), user, user);
            return false;
        }

        if (_timing.CurTime < ent.Comp.NextRefill)
        {
            var left = Math.Ceiling((ent.Comp.NextRefill - _timing.CurTime).TotalSeconds);
            _popup.PopupEntity(Loc.GetString("adt-everfull-not-ready", ("seconds", left)), user, user);
            return false;
        }

        return true;
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<ADTEverfullComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.Filling is not { } reagent)
                continue;

            if (_timing.CurTime < comp.NextFillTick)
                continue;

            comp.NextFillTick = _timing.CurTime + comp.FillInterval;

            if (!_solution.TryGetSolution(uid, comp.Solution, out var soln, out var solution))
            {
                comp.Filling = null;
                continue;
            }

            if (solution.AvailableVolume <= 0)
            {
                comp.Filling = null;
                continue;
            }

            _solution.TryAddReagent(soln.Value, reagent, comp.FillAmount);
        }
    }
}
