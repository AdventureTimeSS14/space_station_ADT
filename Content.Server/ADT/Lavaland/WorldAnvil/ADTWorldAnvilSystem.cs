using Content.Shared.ADT.Lavaland.WorldAnvil;
using Content.Shared.ADT.Mining.Components;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.GameObjects;

namespace Content.Server.ADT.Lavaland.WorldAnvil;

public sealed class ADTWorldAnvilSystem : EntitySystem
{
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPointLightSystem _light = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTWorldAnvilComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ADTWorldAnvilComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<ADTWorldAnvilComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<ADTWorldAnvilComponent, ADTWorldAnvilForgeDoAfterEvent>(OnForgeDoAfter);
    }

    private void OnMapInit(Entity<ADTWorldAnvilComponent> anvil, ref MapInitEvent args)
    {
        UpdateVisuals(anvil);
    }

    private void OnExamined(Entity<ADTWorldAnvilComponent> anvil, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("adt-world-anvil-examine-charges", ("charges", anvil.Comp.Charges)));
    }

    private void OnInteractUsing(Entity<ADTWorldAnvilComponent> anvil, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (TryAddFuel(anvil, args.Used, args.User))
        {
            args.Handled = true;
            return;
        }

        args.Handled = TryStartForging(anvil, args.Used, args.User);
    }

    private bool TryAddFuel(Entity<ADTWorldAnvilComponent> anvil, EntityUid used, EntityUid user)
    {
        foreach (var fuel in anvil.Comp.Fuels)
        {
            if (!_whitelist.IsValid(fuel.Whitelist, used))
                continue;

            anvil.Comp.Charges += GetFuelCharges(used, fuel.Charges);
            QueueDel(used);
            UpdateVisuals(anvil);

            _popup.PopupEntity(
                Loc.GetString("adt-world-anvil-heated", ("charges", anvil.Comp.Charges)),
                anvil,
                user);

            return true;
        }

        return false;
    }

    private int GetFuelCharges(EntityUid used, int fallback)
    {
        if (!TryComp<GibtoniteComponent>(used, out var gibtonite) || gibtonite.ReactionMaxTime <= 0f)
            return fallback;

        var risk = gibtonite.ReactionElapsedTime / gibtonite.ReactionMaxTime;

        if (risk >= 0.9f)
            return 3;

        if (risk >= 0.5f)
            return 2;

        return 1;
    }

    private bool TryStartForging(Entity<ADTWorldAnvilComponent> anvil, EntityUid used, EntityUid user)
    {
        for (var i = 0; i < anvil.Comp.Recipes.Count; i++)
        {
            var recipe = anvil.Comp.Recipes[i];

            if (!_whitelist.IsValid(recipe.Whitelist, used))
                continue;

            if (anvil.Comp.Forging)
            {
                _popup.PopupEntity(Loc.GetString("adt-world-anvil-busy"), anvil, user);
                return true;
            }

            if (anvil.Comp.Charges < recipe.Cost)
            {
                _popup.PopupEntity(Loc.GetString("adt-world-anvil-too-cold"), anvil, user);
                return true;
            }

            var doAfter = new DoAfterArgs(EntityManager,
                user,
                recipe.Delay,
                new ADTWorldAnvilForgeDoAfterEvent(i),
                anvil,
                target: anvil,
                used: used)
            {
                BreakOnMove = true,
                NeedHand = true,
            };

            if (!_doAfter.TryStartDoAfter(doAfter))
                return true;

            anvil.Comp.Forging = true;
            _audio.PlayPvs(anvil.Comp.ForgeStartSound, anvil);

            return true;
        }

        return false;
    }

    private void OnForgeDoAfter(Entity<ADTWorldAnvilComponent> anvil, ref ADTWorldAnvilForgeDoAfterEvent args)
    {
        anvil.Comp.Forging = false;

        if (args.Handled || args.Cancelled)
            return;

        if (args.Used is not { } used || TerminatingOrDeleted(used))
            return;

        if (args.RecipeIndex < 0 || args.RecipeIndex >= anvil.Comp.Recipes.Count)
            return;

        var recipe = anvil.Comp.Recipes[args.RecipeIndex];

        if (anvil.Comp.Charges < recipe.Cost)
        {
            _popup.PopupEntity(Loc.GetString("adt-world-anvil-too-cold"), anvil, args.User);
            return;
        }

        anvil.Comp.Charges -= recipe.Cost;
        QueueDel(used);
        Spawn(recipe.Result, Transform(anvil).Coordinates);

        _audio.PlayPvs(anvil.Comp.ForgeEndSound, anvil);
        _popup.PopupEntity(Loc.GetString("adt-world-anvil-forged"), anvil, args.User);

        UpdateVisuals(anvil);

        if (anvil.Comp.Charges <= 0)
            _popup.PopupEntity(Loc.GetString("adt-world-anvil-cooled"), anvil, args.User);

        args.Handled = true;
    }

    private void UpdateVisuals(Entity<ADTWorldAnvilComponent> anvil)
    {
        var heated = anvil.Comp.Charges > 0;

        _appearance.SetData(anvil, ADTWorldAnvilVisuals.Heated, heated);

        if (_light.TryGetLight(anvil, out var light))
            _light.SetEnabled(anvil, heated, light);
    }
}
