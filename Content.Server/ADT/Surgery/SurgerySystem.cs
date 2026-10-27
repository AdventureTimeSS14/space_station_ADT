using Content.Server.Body.Systems;
using Content.Server.Chat.Systems;
using Content.Shared.ADT.Surgery;
using Content.Shared.ADT.Surgery.Components;
using Content.Shared.ADT.Surgery.Events;
using Content.Shared.Body;
using Content.Shared.Chat;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Standing;
using Content.Shared.StatusEffectNew;
using Content.Shared.Tag;
using Content.Shared.Tools.Systems;
using Content.Shared.Whitelist;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;
using Robust.Shared.Timing;

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
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly StatusEffectsSystem _status = default!;
    [Dependency] private readonly BloodstreamSystem _bloodstream = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly DetachableOrganSystem _detachableOrgan = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedToolSystem _tool = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly ItemToggleSystem _toggle = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SurgeryToolComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<SurgeryChooserComponent, AfterInteractEvent>(OnChooserAfterInteract);
        SubscribeLocalEvent<OperatedComponent, SurgerySelectGraphMessage>(OnSelectGraph);
        SubscribeLocalEvent<OperatedComponent, SurgerySelectEdgeMessage>(OnSelectEdge);
        SubscribeLocalEvent<OperatedComponent, SurgeryStepDoAfterEvent>(OnStepDoAfter);
        SubscribeLocalEvent<OperatedComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<BodyComponent, InteractUsingEvent>(OnBodyInteractUsing);
        SubscribeLocalEvent<SurgeryToolComponent, ExaminedEvent>(OnToolExamined);
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

        if (!_hands.TryGetActiveItem(args.User, out var heldItem))
        {
            _popup.PopupEntity(Loc.GetString("surgery-no-tool"), target, args.User);
            return;
        }

        TryPerformStep(args.User, target, comp, heldItem.Value);
    }

    private void OnInteractUsing(Entity<OperatedComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled || HasComp<SurgeryToolComponent>(args.Used))
            return;

        if (ent.Comp.GraphId == null || string.IsNullOrEmpty(ent.Comp.CurrentNode) || !_standing.IsDown(ent.Owner))
            return;

        if (TryPerformStep(args.User, ent, ent.Comp, args.Used, silent: true))
            args.Handled = true;
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

    private void OnBodyInteractUsing(Entity<BodyComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled || HasComp<SurgeryChooserComponent>(args.Used) || HasComp<SurgeryToolComponent>(args.Used))
            return;

        if (!_standing.IsDown(ent.Owner) || !TryMatchQuality(DrapingQuality, args.Used, out _))
            return;

        args.Handled = true;

        EnsureComp<OperatedComponent>(ent);
        OpenGraphChoice(args.User, ent);
    }
}
