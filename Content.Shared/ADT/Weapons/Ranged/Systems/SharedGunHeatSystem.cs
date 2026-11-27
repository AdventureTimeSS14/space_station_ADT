using Content.Shared.ADT.Weapons.Ranged.Components;
using Content.Shared.Examine;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Item;
using Content.Shared.Popups;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Wieldable;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.ADT.Weapons.Ranged.Systems;

public abstract partial class SharedGunHeatSystem : EntitySystem
{
    [Dependency] protected IGameTiming Timing = default!;
    [Dependency] protected SharedAudioSystem Audio = default!;
    [Dependency] protected SharedPopupSystem Popup = default!;
    [Dependency] private SharedGunSystem _gun = default!;
    [Dependency] private InventorySystem _inventory = default!;

    /// <summary>
    /// Salt for the jam chance random number generator. This is used to ensure that the jam chance is consistent across clients and servers.
    /// </summary>
    private const int JamSalt = -3;

    /// <summary>
    /// How strongly the barrel glows at the given temperature, from 0 to 1.
    /// </summary>
    public static float GetGlow(GunHeatComponent comp, float temperature)
    {
        var ceiling = comp.FailureTemperature > comp.GlowTemperature ? comp.FailureTemperature : comp.MeltTemperature;
        var range = ceiling - comp.GlowTemperature;
        return range <= 0f ? 0f : Math.Clamp((temperature - comp.GlowTemperature) / range, 0f, 1f);
    }

    public static float GetJamChance(GunHeatComponent comp, float temperature)
    {
        var range = comp.MeltTemperature - comp.JamTemperature;
        if (temperature < comp.JamTemperature || range <= 0f)
            return 0f;

        return comp.MaxJamChance * Math.Clamp((temperature - comp.JamTemperature) / range, 0f, 1f);
    }

    [SubscribeLocalEvent]
    private void OnGunShot(Entity<GunHeatComponent> ent, ref GunShotEvent args)
    {
        if (args.Ammo.Count == 0)
            return;

        var rate = ent.Comp.HeatRate;
        ent.Comp.Temperature += ent.Comp.TemperaturePerShot * args.Ammo.Count * rate;
        AddShotHeat(ent, args.Ammo.Count, rate);

        if (!ent.Comp.Failed
            && ent.Comp.FailureTemperature > 0f
            && ent.Comp.Temperature >= ent.Comp.FailureTemperature)
        {
            ent.Comp.Failed = true;
            Audio.PlayPredicted(ent.Comp.MeltSound, ent, args.User);
            Popup.PopupClient(Loc.GetString("gun-heat-failed", ("gun", ent.Owner)), ent, args.User, PopupType.LargeCaution);
            ent.Comp.NextPopupTime = Timing.CurTime + ent.Comp.PopupCooldown;
        }

        if (!ent.Comp.Jammed && !ent.Comp.Failed)
        {
            var heatChance = GetJamChance(ent.Comp, ent.Comp.Temperature);
            var roll = _gun.GetShotRandom(ent, JamSalt).NextDouble();
            if (roll < ent.Comp.BaseJamChance + heatChance)
            {
                ent.Comp.Jammed = true;
                Audio.PlayPredicted(ent.Comp.JamSound, ent, args.User);
                var message = roll < ent.Comp.BaseJamChance ? "gun-heat-jammed-round" : "gun-heat-jammed";
                Popup.PopupClient(Loc.GetString(message), ent, args.User, PopupType.MediumCaution);
                ent.Comp.NextPopupTime = Timing.CurTime + ent.Comp.PopupCooldown;
            }
        }

        Dirty(ent);
    }

    protected virtual void AddShotHeat(Entity<GunHeatComponent> ent, int shots, float rate) { }

    [SubscribeLocalEvent]
    private void OnAttemptShoot(Entity<GunHeatComponent> ent, ref AttemptShootEvent args)
    {
        if (ent.Comp.Failed)
        {
            args.Cancelled = true;
            if (Timing.CurTime < ent.Comp.NextPopupTime)
                return;

            ent.Comp.NextPopupTime = Timing.CurTime + ent.Comp.PopupCooldown;
            Popup.PopupClient(Loc.GetString("gun-heat-failed-blocked"), ent, args.User, PopupType.SmallCaution);
            return;
        }

        if (!ent.Comp.Jammed)
            return;

        args.Cancelled = true;

        if (Timing.CurTime < ent.Comp.NextPopupTime)
            return;

        ent.Comp.NextPopupTime = Timing.CurTime + ent.Comp.PopupCooldown;
        Popup.PopupClient(Loc.GetString("gun-heat-jammed-blocked"), ent, args.User, PopupType.SmallCaution);
    }

    [SubscribeLocalEvent(before: [typeof(SharedWieldableSystem), typeof(SharedGunSystem), typeof(BatteryWeaponFireModesSystem)])]
    private void OnUseInHand(Entity<GunHeatComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled || !ent.Comp.Jammed)
            return;

        if (ent.Comp.Failed)
            return;

        args.Handled = true;
        ent.Comp.Jammed = false;
        Dirty(ent);

        Audio.PlayPredicted(ent.Comp.UnjamSound, ent, args.User);
        Popup.PopupClient(Loc.GetString("gun-heat-unjammed"), ent, args.User);
    }

    [SubscribeLocalEvent]
    private void OnExamined(Entity<GunHeatComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        if (ent.Comp.Failed)
            args.PushMarkup(Loc.GetString("gun-heat-examine-failed"));
        else if (ent.Comp.Jammed)
            args.PushMarkup(Loc.GetString("gun-heat-examine-jammed"));

        var temperature = ent.Comp.Temperature;
        if (temperature >= ent.Comp.JamTemperature + ((ent.Comp.MeltTemperature - ent.Comp.JamTemperature) * 0.5f))
            args.PushMarkup(Loc.GetString("gun-heat-examine-critical"));
        else if (temperature >= ent.Comp.JamTemperature)
            args.PushMarkup(Loc.GetString("gun-heat-examine-overheated"));
        else if (temperature >= ent.Comp.GlowTemperature)
            args.PushMarkup(Loc.GetString("gun-heat-examine-hot"));
        else if (temperature >= ent.Comp.GlowTemperature - 30f)
            args.PushMarkup(Loc.GetString("gun-heat-examine-warm"));
    }

    [SubscribeLocalEvent]
    private void OnGettingPickedUp(Entity<GunHeatComponent> ent, ref GettingPickedUpAttemptEvent args)
    {
        if (ent.Comp.UnsafePickupTemperature <= 0f || ent.Comp.Temperature < ent.Comp.UnsafePickupTemperature)
            return;

        if (_inventory.TryGetSlotEntity(args.User, "gloves", out _))
            return;

        args.Cancel();
        if (!args.ShowPopup)
            return;

        Popup.PopupClient(Loc.GetString("gun-heat-too-hot", ("gun", ent.Owner)), args.User, args.User);
    }
}
