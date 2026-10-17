using Content.Shared.Database;
using Content.Shared.Eye;
using Content.Shared.Hands;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Timing;
using Content.Shared.Verbs;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Utility;

namespace Content.Shared.SubFloor;

public abstract partial class SharedTrayScannerSystem : EntitySystem
{
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedEyeSystem _eye = default!;
    [Dependency] private UseDelaySystem _delay = default!;
    [Dependency] private INetManager _netMan = default!;
    [Dependency] private SharedContainerSystem _containers = default!; // ADT-Tweak

    public const float SubfloorRevealAlpha = 0.8f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TrayScannerComponent, ActivateInWorldEvent>(OnTrayScannerActivate);
        SubscribeLocalEvent<TrayScannerComponent, GetVerbsEvent<AlternativeVerb>>(OnAddSwitchModeVerb);
        SubscribeLocalEvent<TrayScannerComponent, GotEquippedHandEvent>(OnTrayHandEquipped);
        SubscribeLocalEvent<TrayScannerComponent, GotUnequippedHandEvent>(OnTrayHandUnequipped);
        SubscribeLocalEvent<TrayScannerComponent, GotEquippedEvent>(OnTrayEquipped);
        SubscribeLocalEvent<TrayScannerComponent, GotUnequippedEvent>(OnTrayUnequipped);
        SubscribeLocalEvent<TrayScannerUserComponent, GetVisMaskEvent>(OnUserGetVis);
        // ADT-Tweak-Start: МОДули выдают компонент уже надетой вещи, события надевания при этом нет
        SubscribeLocalEvent<TrayScannerComponent, ComponentStartup>(OnTrayStartup);
        SubscribeLocalEvent<TrayScannerComponent, ComponentShutdown>(OnTrayShutdown);
        // ADT-Tweak-End
    }

    private void OnAddSwitchModeVerb(Entity<TrayScannerComponent> scanner, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || !args.Using.HasValue || !scanner.Comp.Enabled)
            return;

        var user = args.User;

        AlternativeVerb verb = new()
        {
            Text = Loc.GetString("tray-scanner-switch-mode"),
            Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/settings.svg.192dpi.png")),
            Act = () => SwitchMode(scanner, user),
            Impact = LogImpact.Low
        };
        args.Verbs.Add(verb);
    }

    private static TrayScannerMode Next(TrayScannerMode mode)
    {
        return mode switch
        {
            TrayScannerMode.All => TrayScannerMode.Wiring,
            TrayScannerMode.Wiring => TrayScannerMode.Piping,
            TrayScannerMode.Piping => TrayScannerMode.All,
            _ => TrayScannerMode.All,
        };
    }

    private void SwitchMode(Entity<TrayScannerComponent> scanner, EntityUid? userUid)
    {
        if (!userUid.HasValue)
            return;

        // Prevents ping spam
        if (!_delay.TryResetDelay(scanner, checkDelayed: true))
            return;

        scanner.Comp.Mode = Next(scanner.Comp.Mode);
        Dirty(scanner);

        // Play a slightly different sound when we're back to All mode
        var pitch = scanner.Comp.Mode == TrayScannerMode.All ? 1 : 0.8f;
        _audio.PlayPredicted(scanner.Comp.SoundSwitchMode, scanner, userUid, AudioParams.Default.WithVolume(1.5f).WithPitchScale(pitch));
    }

    private void OnUserGetVis(Entity<TrayScannerUserComponent> scanner, ref GetVisMaskEvent args)
    {
        args.VisibilityMask |= (int)VisibilityFlags.Subfloor;
    }

    private void OnEquip(EntityUid user)
    {
        if (_netMan.IsClient)
            return;

        var comp = EnsureComp<TrayScannerUserComponent>(user);
        comp.Count++;

        if (comp.Count > 1)
            return;

        _eye.RefreshVisibilityMask(user);
    }

    private void OnUnequip(EntityUid user)
    {
        if (_netMan.IsClient)
            return;

        if (!TryComp(user, out TrayScannerUserComponent? comp))
            return;

        comp.Count--;

        if (comp.Count > 0)
            return;

        RemComp<TrayScannerUserComponent>(user);
        _eye.RefreshVisibilityMask(user);
    }

    // ADT-Tweak-Start
    /// <summary>
    /// Счётчик носителя ведётся вручную по событиям надевания, поэтому компонент,
    /// выданный или снятый на уже надетой вещи, нужно учесть отдельно.
    /// </summary>
    private void OnTrayStartup(Entity<TrayScannerComponent> ent, ref ComponentStartup args)
    {
        if (TryGetWearer(ent, out var user))
            OnEquip(user);
    }

    private void OnTrayShutdown(Entity<TrayScannerComponent> ent, ref ComponentShutdown args)
    {
        if (TryGetWearer(ent, out var user))
            OnUnequip(user);
    }

    private bool TryGetWearer(EntityUid uid, out EntityUid user)
    {
        user = default;

        if (!_containers.TryGetContainingContainer((uid, null), out var container)
            || !HasComp<InventoryComponent>(container.Owner))
        {
            return false;
        }

        user = container.Owner;
        return true;
    }
    // ADT-Tweak-End

    private void OnTrayHandUnequipped(Entity<TrayScannerComponent> ent, ref GotUnequippedHandEvent args)
    {
        OnUnequip(args.User);
    }

    private void OnTrayHandEquipped(Entity<TrayScannerComponent> ent, ref GotEquippedHandEvent args)
    {
        OnEquip(args.User);
    }

    private void OnTrayUnequipped(Entity<TrayScannerComponent> ent, ref GotUnequippedEvent args)
    {
        OnUnequip(args.EquipTarget);
    }

    private void OnTrayEquipped(Entity<TrayScannerComponent> ent, ref GotEquippedEvent args)
    {
        OnEquip(args.EquipTarget);
    }

    private void OnTrayScannerActivate(Entity<TrayScannerComponent> ent, ref ActivateInWorldEvent args)
    {
        if (args.Handled || !args.Complex)
            return;

        ent.Comp.Enabled = !ent.Comp.Enabled;
        Dirty(ent);

        if (TryComp<AppearanceComponent>(ent, out var appearance))
        {
            _appearance.SetData(ent, TrayScannerVisual.Visual, ent.Comp.Enabled ? TrayScannerVisual.On : TrayScannerVisual.Off, appearance);
        }

        args.Handled = true;
    }
}
