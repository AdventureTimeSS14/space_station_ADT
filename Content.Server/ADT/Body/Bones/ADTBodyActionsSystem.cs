using System.Linq;
using Content.Server.ADT.Body.Shrapnel;
using Content.Shared.ADT.Body.Bones;
using Content.Shared.ADT.Body.Shrapnel;
using Content.Shared.Body;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.Utility;

namespace Content.Server.ADT.Body.Bones;

public sealed class ADTBodyActionsSystem : EntitySystem
{
    [Dependency] private ADTFractureSystem _fracture = default!;
    [Dependency] private ADTShrapnelSystem _shrapnel = default!;
    [Dependency] private UserInterfaceSystem _ui = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BodyComponent, ADTBodyActionSelectMessage>(OnSelect);
        SubscribeLocalEvent<BodyComponent, GetVerbsEvent<InteractionVerb>>(OnGetVerbs);
        SubscribeLocalEvent<ADTEmbeddedCarrierComponent, ADTEmbeddedShrapnelAlertEvent>(OnShrapnelAlert);
    }

    private void OnShrapnelAlert(Entity<ADTEmbeddedCarrierComponent> ent, ref ADTEmbeddedShrapnelAlertEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        Open(args.User, ent.Owner);
    }

    private void OnGetVerbs(Entity<BodyComponent> ent, ref GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || !args.CanComplexInteract)
            return;

        var user = args.User;
        var target = ent.Owner;
        if (GetOptions(user, target).Count == 0)
            return;

        args.Verbs.Add(new InteractionVerb
        {
            Text = Loc.GetString("adt-body-action-verb"),
            Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/plus.svg.192dpi.png")),
            Act = () => Open(user, target),
        });
    }

    public void Open(EntityUid user, EntityUid target)
    {
        var options = GetOptions(user, target);
        if (options.Count == 0)
        {
            _popup.PopupEntity(Loc.GetString("adt-body-action-nothing"), target, user);
            return;
        }

        if (options.Count == 1)
        {
            Execute(user, target, GetEntity(options[0].Organ), options[0].Action);
            return;
        }

        _ui.SetUi(target, ADTBodyActionUiKey.Key, new InterfaceData("ADTBodyActionsBoundUserInterface"));

        foreach (var actor in _ui.GetActors(target, ADTBodyActionUiKey.Key).ToList())
        {
            if (actor != user)
                _ui.CloseUi(target, ADTBodyActionUiKey.Key, actor);
        }

        _ui.OpenUi(target, ADTBodyActionUiKey.Key, user);
        _ui.SetUiState(target, ADTBodyActionUiKey.Key, new ADTBodyActionsBuiState(options));
    }

    public List<ADTBodyActionOption> GetOptions(EntityUid user, EntityUid target)
    {
        var options = new List<ADTBodyActionOption>();
        var held = _hands.GetActiveItemOrSelf(user);
        var splint = HasComp<ADTSplintComponent>(held);
        var tool = held != user && _shrapnel.IsExtractTool(held, out _, out _) ? held : (EntityUid?) null;

        foreach (var (bone, _) in _fracture.EnumerateBones(target))
        {
            if (splint && _fracture.CanSplint(bone))
                options.Add(Option(bone.Owner, ADTBodyAction.ApplySplint, "adt-body-action-apply-splint"));

            if (bone.Comp.Splinted)
                options.Add(Option(bone.Owner, ADTBodyAction.RemoveSplint, "adt-body-action-remove-splint"));

            if (_fracture.GetFractureType(bone.Comp) is { Open: true, ReattachInto: not null })
                options.Add(Option(bone.Owner, ADTBodyAction.Reattach, "adt-body-action-reattach"));
        }

        foreach (var (organ, _) in _shrapnel.EnumerateEmbedded(target))
        {
            options.Add(Option(organ, ADTBodyAction.RemoveShrapnelHands, "adt-body-action-shrapnel-hands"));

            if (tool != null)
                options.Add(Option(organ, ADTBodyAction.RemoveShrapnelTool, "adt-body-action-shrapnel-tool", tool.Value));
        }

        return options;
    }

    private ADTBodyActionOption Option(EntityUid organ, ADTBodyAction action, string loc, EntityUid? tool = null)
    {
        var limb = Name(organ);
        var label = tool == null
            ? Loc.GetString(loc, ("limb", limb))
            : Loc.GetString(loc, ("limb", limb), ("tool", tool.Value));

        return new ADTBodyActionOption(GetNetEntity(organ), action, limb, label);
    }

    private void OnSelect(Entity<BodyComponent> ent, ref ADTBodyActionSelectMessage args)
    {
        var actor = args.Actor;
        var netOrgan = args.Organ;
        var action = args.Action;

        _ui.CloseUi(ent.Owner, ADTBodyActionUiKey.Key, actor);

        var organ = GetEntity(netOrgan);
        var allowed = GetOptions(actor, ent.Owner).Any(o => o.Organ == netOrgan && o.Action == action);
        if (!allowed)
            return;

        Execute(actor, ent.Owner, organ, action);
    }

    private void Execute(EntityUid user, EntityUid target, EntityUid organ, ADTBodyAction action)
    {
        var held = _hands.GetActiveItemOrSelf(user);

        switch (action)
        {
            case ADTBodyAction.ApplySplint:
                if (TryComp<ADTSplintComponent>(held, out var splint))
                    _fracture.StartSplint((held, splint), user, target, organ);
                break;
            case ADTBodyAction.RemoveSplint:
                _fracture.StartSplintRemove(user, target, organ);
                break;
            case ADTBodyAction.Reattach:
                _fracture.StartReattach(user, target, organ);
                break;
            case ADTBodyAction.RemoveShrapnelHands:
                _shrapnel.StartRemove(user, target, organ, null);
                break;
            case ADTBodyAction.RemoveShrapnelTool:
                if (held != user)
                    _shrapnel.StartRemove(user, target, organ, held);
                break;
        }
    }
}
