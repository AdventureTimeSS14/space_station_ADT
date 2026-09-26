using Content.Server.Radiation.Systems;
using Content.Server.Stack;
using Content.Server.Temperature.Systems;
using Content.Shared.ADT.Lavaland;
using Content.Shared.ADT.Lavaland.Components;
using Content.Shared.ADT.Salvage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Kitchen.Components;
using Content.Shared.Mining.Components;
using Content.Shared.Popups;
using Content.Shared.Temperature.Components;
using Content.Shared.Tools.Systems;

namespace Content.Server.ADT.Lavaland;

public sealed class ADTGemSystem : EntitySystem
{
    [Dependency] private readonly MiningPointsSystem _points = default!;
    [Dependency] private readonly RadiationSystem _radiation = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedToolSystem _tool = default!;
    [Dependency] private readonly StackSystem _stack = default!;
    [Dependency] private readonly TemperatureSystem _temperature = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTGemComponent, InteractUsingEvent>(OnGemInteract);
        SubscribeLocalEvent<ADTGemComponent, ADTGemWeldDoAfterEvent>(OnWeldDoAfter);

        SubscribeLocalEvent<ADTGemTemperatureComponent, UseInHandEvent>(OnTemperatureUse);

        SubscribeLocalEvent<ADTRuperiumComponent, InteractUsingEvent>(OnRuperiumInteract);
        SubscribeLocalEvent<ADTRuperiumComponent, ADTRuperiumCutDoAfterEvent>(OnRuperiumCut);
    }

    private void OnGemInteract(Entity<ADTGemComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (HasComp<MiningScannerComponent>(args.Used))
        {
            Analyse(ent, args.User);
            args.Handled = true;
            return;
        }

        if (ent.Comp.Sheet == null || !_tool.HasQuality(args.Used, SharedToolSystem.WeldingQuality))
            return;

        args.Handled = _tool.UseTool(args.Used,
            args.User,
            ent.Owner,
            ent.Comp.WeldDelay,
            SharedToolSystem.WeldingQuality,
            new ADTGemWeldDoAfterEvent());
    }

    private void Analyse(Entity<ADTGemComponent> ent, EntityUid user)
    {
        if (ent.Comp.Analysed)
        {
            _popup.PopupEntity(Loc.GetString("adt-gem-already-analysed"), ent.Owner, user);
            return;
        }

        ent.Comp.Analysed = true;
        _appearance.SetData(ent.Owner, ADTGemVisuals.Analysed, true);

        if (_points.TryFindIdCard(user) is not { } card)
        {
            _popup.PopupEntity(Loc.GetString("adt-gem-analysed"), ent.Owner, user);
            return;
        }

        _points.AddPoints(card, ent.Comp.PointValue);
        _popup.PopupEntity(Loc.GetString("adt-gem-analysed-points", ("points", ent.Comp.PointValue)), ent.Owner, user);
    }

    private void OnWeldDoAfter(Entity<ADTGemComponent> ent, ref ADTGemWeldDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || ent.Comp.Sheet is not { } sheet)
            return;

        args.Handled = true;

        if (EntityManager.IsQueuedForDeletion(ent.Owner))
            return;

        _stack.SpawnMultipleNextToOrDrop(sheet, ent.Comp.SheetAmount, ent.Owner);
        QueueDel(ent.Owner);
    }

    private void OnTemperatureUse(Entity<ADTGemTemperatureComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled || !TryComp<TemperatureComponent>(args.User, out var temperature))
            return;

        args.Handled = true;
        _temperature.ForceChangeTemperature(args.User, temperature.CurrentTemperature + ent.Comp.Delta, temperature);
        _popup.PopupEntity(Loc.GetString(ent.Comp.Message, ("user", args.User), ("gem", ent.Owner)), args.User, PopupType.Small);
    }

    private void OnRuperiumInteract(Entity<ADTRuperiumComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled || !ent.Comp.Shielded || !HasComp<SharpComponent>(args.Used))
            return;

        var doAfter = new DoAfterArgs(EntityManager, args.User, ent.Comp.CutDelay, new ADTRuperiumCutDoAfterEvent(), ent.Owner, ent.Owner, args.Used)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
        };

        if (!_doAfter.TryStartDoAfter(doAfter))
            return;

        _popup.PopupEntity(Loc.GetString("adt-gem-ruperium-cut-start"), args.User, args.User);
        args.Handled = true;
    }

    private void OnRuperiumCut(Entity<ADTRuperiumComponent> ent, ref ADTRuperiumCutDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || !ent.Comp.Shielded)
            return;

        args.Handled = true;
        ent.Comp.Shielded = false;

        _appearance.SetData(ent.Owner, ADTGemVisuals.Broken, true);
        _radiation.SetSourceEnabled(ent.Owner, true);
        _popup.PopupEntity(Loc.GetString("adt-gem-ruperium-cut"), args.User, args.User, PopupType.MediumCaution);
    }
}
