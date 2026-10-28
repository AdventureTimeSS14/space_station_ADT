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
using Content.Shared.Tools.Systems;
using Content.Shared.Whitelist;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Surgery;

public sealed partial class SurgerySystem : SharedSurgerySystem
{
    [Dependency] private UserInterfaceSystem _ui = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private StandingStateSystem _standing = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private ChatSystem _chat = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private StatusEffectsSystem _status = default!;
    [Dependency] private BloodstreamSystem _bloodstream = default!;
    [Dependency] private IGameTiming _gameTiming = default!;
    [Dependency] private DetachableOrganSystem _detachableOrgan = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private SharedToolSystem _tool = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;
    [Dependency] private ItemToggleSystem _toggle = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SurgeryToolComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<BodyComponent, SurgerySelectGraphMessage>(OnSelectGraph);
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

        if (!TryComp<OperatedComponent>(target, out var comp) || comp.GraphId == null)
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

        if (ent.Comp.GraphId == null || !_standing.IsDown(ent.Owner))
            return;

        if (TryPerformStep(args.User, ent, ent.Comp, args.Used, silent: true))
            args.Handled = true;
    }

    private void OnBodyInteractUsing(Entity<BodyComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled || HasComp<SurgeryToolComponent>(args.Used))
            return;

        if (!_standing.IsDown(ent.Owner) || !TryMatchQuality(DrapingQuality, args.Used, out _))
            return;

        args.Handled = true;

        OpenGraphChoice(args.User, ent);
    }
}
