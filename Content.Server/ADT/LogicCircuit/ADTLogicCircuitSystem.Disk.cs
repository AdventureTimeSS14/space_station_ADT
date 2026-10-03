using Content.Server.ADT.LogicCircuit.Components;
using Content.Shared.ADT.LogicCircuit;
using Content.Shared.ADT.LogicCircuit.Components;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Verbs;

namespace Content.Server.ADT.LogicCircuit;

public sealed partial class ADTLogicCircuitSystem
{
    partial void InitializeDisk()
    {
        SubscribeLocalEvent<ADTLogicCircuitComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<ADTLogicCircuitComponent, ExaminedEvent>(OnExamined);

        SubscribeLocalEvent<ADTLogicDiskComponent, ExaminedEvent>(OnDiskExamined);
        SubscribeLocalEvent<ADTLogicDiskComponent, GetVerbsEvent<Verb>>(OnDiskVerbs);
    }

    private void OnInteractUsing(EntityUid uid, ADTLogicCircuitComponent comp, InteractUsingEvent args)
    {
        if (args.Handled || !TryComp<ADTLogicDiskComponent>(args.Used, out var disk))
            return;

        var ent = (uid, comp);

        if (disk.Layout == null)
        {
            disk.Layout = comp.Layout.Clone();
            disk.Label = Name(uid);

            _popup.PopupEntity(Loc.GetString("logic-circuit-disk-saved"), uid, args.User);
            args.Handled = true;
            return;
        }

        if (TryApplyLayout(ent, disk.Layout, out var error, out var detail))
        {
            UpdateUiState(ent);
            _popup.PopupEntity(Loc.GetString("logic-circuit-disk-loaded"), uid, args.User);
            args.Handled = true;
            return;
        }

        _popup.PopupEntity(LogicCircuitErrors.GetMessage(error, detail), uid, args.User);
        args.Handled = true;
    }

    private void OnExamined(EntityUid uid, ADTLogicCircuitComponent comp, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        if (comp.Broken)
        {
            args.PushMarkup(Loc.GetString("logic-circuit-examine-broken"));
            return;
        }

        args.PushMarkup(Loc.GetString("logic-circuit-examine",
            ("elements", comp.Layout.Nodes.Count),
            ("used", GetPowerUsed(comp.Layout)),
            ("total", comp.PowerBudget)));

        if (!comp.Enabled)
            args.PushMarkup(Loc.GetString("logic-circuit-examine-disabled"));
    }

    private void OnDiskExamined(EntityUid uid, ADTLogicDiskComponent comp, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        if (comp.Layout == null)
        {
            args.PushMarkup(Loc.GetString("logic-disk-examine-empty"));
            return;
        }

        args.PushMarkup(Loc.GetString("logic-disk-examine",
            ("label", comp.Label),
            ("elements", comp.Layout.Nodes.Count)));
    }

    private void OnDiskVerbs(EntityUid uid, ADTLogicDiskComponent comp, GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract || comp.Layout == null)
            return;

        var user = args.User;

        args.Verbs.Add(new Verb
        {
            Text = Loc.GetString("logic-disk-verb-erase"),
            Act = () =>
            {
                comp.Layout = null;
                comp.Label = string.Empty;
                _popup.PopupEntity(Loc.GetString("logic-disk-erased"), uid, user);
            },
        });
    }
}
