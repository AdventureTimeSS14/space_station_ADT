using Content.Shared.ADT.Surgery;
using Content.Shared.ADT.Surgery.Components;
using Content.Shared.ADT.Surgery.Events;
using Content.Shared.Body;
using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Standing;
using Content.Shared.Tag;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;

namespace Content.Server.ADT.Surgery;

public sealed partial class SurgerySystem : SharedSurgerySystem
{
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly BodySystem _body = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SurgeryToolComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<SurgeryChooserComponent, AfterInteractEvent>(OnChooserAfterInteract);
        SubscribeLocalEvent<OperatedComponent, SurgerySelectGraphMessage>(OnSelectGraph);
        SubscribeLocalEvent<OperatedComponent, SurgerySelectEdgeMessage>(OnSelectEdge);
        SubscribeLocalEvent<OperatedComponent, SurgeryStepDoAfterEvent>(OnStepDoAfter);
    }

    private void OnAfterInteract(EntityUid uid, SurgeryToolComponent component, AfterInteractEvent args)
    {
        if (args.Handled || args.Target is not { } target || !args.CanReach)
            return;

        if (!HasComp<BodyComponent>(target) || !_standing.IsDown(target))
            return;

        args.Handled = true;

        if (!TryComp<OperatedComponent>(target, out var comp) || comp.GraphId == null || string.IsNullOrEmpty(comp.CurrentNode))
        {
            _popup.PopupEntity(Loc.GetString("surgery-choose-with-drapes"), target, args.User, PopupType.MediumCaution);
            return;
        }

        TryPerformStep(args.User, target, comp);
    }

    private void OnChooserAfterInteract(EntityUid uid, SurgeryChooserComponent component, AfterInteractEvent args)
    {
        if (args.Handled || args.Target is not { } target || !args.CanReach)
            return;

        if (!HasComp<BodyComponent>(target) || !_standing.IsDown(target))
            return;

        args.Handled = true;

        EnsureComp<OperatedComponent>(target);
        OpenGraphChoice(args.User, target);
    }
}
