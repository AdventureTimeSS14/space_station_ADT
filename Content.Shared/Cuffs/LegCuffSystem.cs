using Content.Shared.Cuffs.Components;
using Content.Shared.DoAfter;
using Content.Shared.Ensnaring;
using Content.Shared.Ensnaring.Components;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Components;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.Cuffs;

public sealed partial class LegCuffSystem : EntitySystem
{
    [Dependency] private SharedEnsnareableSystem _ensnareable = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private IGameTiming _timing = default!;

    private readonly Dictionary<EntityUid, TimeSpan> _lastBreakoutSound = new();
    private static readonly TimeSpan BreakoutSoundCooldown = TimeSpan.FromSeconds(1);

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LegCuffComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<LegCuffComponent, LegCuffDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<LegCuffComponent, EntGotRemovedFromContainerMessage>(OnRemovedFromContainer);
        SubscribeLocalEvent<EnsnareableComponent, GetVerbsEvent<InteractionVerb>>(OnGetVerbs);
        SubscribeLocalEvent<LegCuffedComponent, RemoveEnsnareAlertEvent>(OnSelfBreakoutAttempt);
    }

    private void OnAfterInteract(EntityUid uid, LegCuffComponent comp, AfterInteractEvent args)
    {
        if (args.Target is not { } target || !args.CanReach || args.Handled)
            return;

        if (!HasComp<MobStateComponent>(target))
            return;

        if (!TryComp<EnsnareableComponent>(target, out var ensnareableCheck))
            return;

        if (ensnareableCheck.IsEnsnared)
            return;

        _audio.PlayPredicted(comp.StartCuffSound, target, args.User);

        var doAfterArgs = new DoAfterArgs(EntityManager, args.User, comp.ApplyDelay,
            new LegCuffDoAfterEvent(), uid, target: target, used: uid)
        {
            BreakOnMove = true,
            NeedHand = true,
            BreakOnDamage = true,
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
        args.Handled = true;
    }

    private void OnDoAfter(EntityUid uid, LegCuffComponent comp, LegCuffDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Args.Target is not { } target)
            return;

        if (!TryComp<EnsnaringComponent>(uid, out var ensnaring))
            return;

        if (!_ensnareable.TryEnsnare(target, uid, ensnaring))
        {
            args.Handled = true;
            return;
        }

        _lastBreakoutSound.Remove(target);

        var cuffed = EnsureComp<LegCuffedComponent>(target);
        cuffed.CuffedRSI = comp.CuffedRSI;
        cuffed.BodyIconState = comp.BodyIconState;
        Dirty(target, cuffed);

        args.Handled = true;
    }

    private void OnGetVerbs(EntityUid uid, EnsnareableComponent comp, GetVerbsEvent<InteractionVerb> args)
    {
        if (!comp.IsEnsnared)
            return;

        if (args.User == uid)
            return;

        if (!args.CanInteract || !args.CanAccess)
            return;

        EntityUid? legCuffEntity = null;
        foreach (var contained in comp.Container.ContainedEntities)
        {
            if (!HasComp<LegCuffComponent>(contained))
                continue;

            legCuffEntity = contained;
            break;
        }

        if (legCuffEntity == null)
            return;

        var verb = new InteractionVerb
        {
            Text = Loc.GetString("legcuffs-verb-remove"),
            Icon = new SpriteSpecifier.Texture(
                new ResPath("/Textures/Interface/VerbIcons/unlock.svg.192dpi.png")),
            Act = () =>
            {
                if (!TryComp<EnsnaringComponent>(legCuffEntity, out var ensnaring))
                    return;

                if (TryComp<LegCuffComponent>(legCuffEntity, out var legCuff))
                    _audio.PlayPredicted(legCuff.RemoveCuffSound, uid, args.User);

                _ensnareable.TryFree(uid, args.User, legCuffEntity.Value, ensnaring);
            }
        };

        args.Verbs.Add(verb);
    }

    private void OnSelfBreakoutAttempt(EntityUid uid, LegCuffedComponent comp, RemoveEnsnareAlertEvent args)
    {
        if (!TryComp<EnsnareableComponent>(uid, out var ensnareable) || !ensnareable.IsEnsnared)
            return;

        var now = _timing.CurTime;
        if (_lastBreakoutSound.TryGetValue(uid, out var lastTime) && now - lastTime < BreakoutSoundCooldown)
            return;

        _lastBreakoutSound[uid] = now;

        foreach (var contained in ensnareable.Container.ContainedEntities)
        {
            if (!TryComp<LegCuffComponent>(contained, out var legCuff))
                continue;

            _audio.PlayPvs(legCuff.RemoveCuffSound, uid);
            break;
        }
    }

    private void OnRemovedFromContainer(EntityUid uid, LegCuffComponent comp, EntGotRemovedFromContainerMessage args)
    {
        var victim = args.Container.Owner;

        if (!TryComp<EnsnareableComponent>(victim, out var ens))
            return;

        if (!ReferenceEquals(ens.Container, args.Container))
            return;

        RemComp<LegCuffedComponent>(victim);
        _lastBreakoutSound.Remove(victim);
    }
}
