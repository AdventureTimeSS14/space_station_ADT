using Content.Shared.ADT.Silicons.Borgs.Components;
using Content.Shared.Emag.Systems;
using Content.Shared.Mind;
using Content.Shared.Overlays;
using Content.Shared.Popups;
using Content.Shared.Silicons.Borgs;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Silicons.Laws.Components;
using Content.Shared.Stunnable;
using Content.Shared.Wires;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;

namespace Content.Shared.Silicons.Laws;

/// <summary>
/// This handles getting and displaying the laws for silicons.
/// </summary>
public abstract partial class SharedSiliconLawSystem : EntitySystem
{
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedStunSystem _stunSystem = default!;
    [Dependency] private EmagSystem _emag = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private SharedAudioSystem _audio = default!; // ADT-Tweak
    [Dependency] protected SharedBorgSystem _borg = default!; // ADT-Tweak

    /// <summary>
    /// Minimum length of generated ion storm law identifiers.
    /// </summary>
    public const int IonStormIdentifierMinLength = 3;
    /// <summary>
    /// Maximum length of generated ion storm law identifiers.
    /// </summary>
    public const int IonStormIdentifierMaxLength = 10;

    /// <inheritdoc/>
    public override void Initialize()
    {
        InitializeUpdater();
        SubscribeLocalEvent<EmagSiliconLawComponent, GotEmaggedEvent>(OnGotEmagged);
    }

    private void OnGotEmagged(EntityUid uid, EmagSiliconLawComponent component, ref GotEmaggedEvent args)
    {
        // ADT-Tweak-AiRemoteControl-Start
        if (HasComp<AiRemoteControllerComponent>(uid))
            return;
        // ADT-Tweak-AiRemoteControl-End

        if (!_emag.CompareFlag(args.Type, EmagType.Interaction))
            return;

        if (_emag.CheckFlag(uid, EmagType.Interaction))
            return;

        // prevent self-emagging
        if (uid == args.UserUid)
        {
            _popup.PopupEntity(Loc.GetString("law-emag-cannot-emag-self"), uid, args.UserUid);
            return;
        }

        if (component.RequireOpenPanel &&
            TryComp<WiresPanelComponent>(uid, out var panel) &&
            !panel.Open)
        {
            _popup.PopupEntity(Loc.GetString("law-emag-require-panel"), uid, args.UserUid);
            return;
        }

        var ev = new SiliconEmaggedEvent(args.UserUid);
        RaiseLocalEvent(uid, ref ev);

        component.OwnerName = Name(args.UserUid);

        if (!TryComp<SiliconLawProviderComponent>(uid, out var lawcomp))
        // ADT-Tweak start
        {
            if (!_borg.TryGetBorgBrain(uid, out var borgBrain)
                || !HasComp<SiliconLawProviderComponent>(borgBrain))
                return;

            var brainEv = new SiliconEmaggedEvent(args.UserUid);
            RaiseLocalEvent(borgBrain, ref brainEv);

            _audio.PlayPredicted(component.EmaggedSound, uid, args.UserUid);
            _popup.PopupEntity(Loc.GetString("laws-notify-subverted"), uid, uid);

            if (_mind.TryGetMind(uid, out var borgMindId, out _)
                || _mind.TryGetMind(borgBrain, out borgMindId, out _))
                EnsureSubvertedSiliconRole(borgMindId);

            _stunSystem.TryUpdateParalyzeDuration(uid, component.StunTime);

            args.Handled = true;
            return;
        }
        // ADT-Tweak end

        NotifyLawsChanged((uid, lawcomp), component.EmaggedSound);
        if(_mind.TryGetMind(uid, out var mindId, out _))
            EnsureSubvertedSiliconRole(mindId);

        _stunSystem.TryUpdateParalyzeDuration(uid, component.StunTime);

        args.Handled = true;
    }

    public virtual void NotifyLawsChanged(Entity<SiliconLawProviderComponent> ent, SoundSpecifier? cue = null)
    {

    }

    protected virtual void EnsureSubvertedSiliconRole(EntityUid mindId)
    {
        if (TryComp<MindComponent>(mindId, out var mind))
        {
            var owner = mind.OwnedEntity;
            if (TryComp<ShowCrewIconsComponent>(owner, out var crewIconComp))
            {
                crewIconComp.UncertainCrewBorder = true;
                Dirty(owner.Value, crewIconComp);
            }
        }
    }

    protected virtual void RemoveSubvertedSiliconRole(EntityUid mindId)
    {
        if (TryComp<MindComponent>(mindId, out var mind))
        {
            var owner = mind.OwnedEntity;
            if (TryComp<ShowCrewIconsComponent>(owner, out var crewIconComp))
            {
                crewIconComp.UncertainCrewBorder = false;
                Dirty(owner.Value, crewIconComp);
            }
        }
    }
}

[ByRefEvent]
public record struct SiliconEmaggedEvent(EntityUid user);
