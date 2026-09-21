using Content.Shared.Examine;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;

namespace Content.Shared.ADT.Mining.Resonator;

public abstract class SharedADTResonatorSystem : EntitySystem
{
    [Dependency] private readonly SharedBatterySystem _battery = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTResonatorComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<ADTResonatorComponent, ExaminedEvent>(OnExamine);
    }

    private void OnUseInHand(Entity<ADTResonatorComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled || ent.Comp.Modes.Count < 2)
            return;

        args.Handled = true;
        CycleMode(ent, args.User);
    }

    private void OnExamine(Entity<ADTResonatorComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        args.PushMarkup(Loc.GetString("adt-resonator-examine-mode", ("mode", GetModeName(ent.Comp.Mode))));
        args.PushMarkup(Loc.GetString("adt-resonator-examine-limit", ("limit", ent.Comp.FieldLimit)));

        if (!HasComp<BatteryComponent>(ent))
            return;

        var charge = _battery.GetChargeLevel(ent.Owner);
        args.PushMarkup(Loc.GetString("adt-resonator-examine-charge",
            ("percent", MathF.Round(charge * 100f)),
            ("uses", (int)MathF.Floor(charge / ent.Comp.ChargeUsePercent))));
    }

    public void CycleMode(Entity<ADTResonatorComponent> ent, EntityUid user)
    {
        var index = ent.Comp.Modes.IndexOf(ent.Comp.Mode);
        ent.Comp.Mode = ent.Comp.Modes[(index + 1) % ent.Comp.Modes.Count];
        Dirty(ent);

        _popup.PopupClient(Loc.GetString(GetModeDescription(ent.Comp.Mode)), ent, user);
    }

    private string GetModeName(ADTResonatorMode mode)
    {
        return Loc.GetString(mode switch
        {
            ADTResonatorMode.Manual => "adt-resonator-mode-manual",
            ADTResonatorMode.Matrix => "adt-resonator-mode-matrix",
            _ => "adt-resonator-mode-auto",
        });
    }

    private static string GetModeDescription(ADTResonatorMode mode)
    {
        return mode switch
        {
            ADTResonatorMode.Manual => "adt-resonator-set-manual",
            ADTResonatorMode.Matrix => "adt-resonator-set-matrix",
            _ => "adt-resonator-set-auto",
        };
    }
}
