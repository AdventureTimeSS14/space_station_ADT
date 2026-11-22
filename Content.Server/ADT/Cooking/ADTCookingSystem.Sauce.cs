using Content.Shared.ADT.Cooking.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Interaction;

namespace Content.Server.ADT.Cooking;

public sealed partial class ADTCookingSystem
{
    private void InitializeSauce()
    {
        SubscribeLocalEvent<ADTDishComponent, InteractUsingEvent>(OnDishInteractUsing, before: new[] { typeof(SolutionSpikerSystem) });
    }

    private void OnDishInteractUsing(Entity<ADTDishComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled || !HasComp<SolutionTransferComponent>(args.Used))
            return;

        args.Handled = TryPourSauce(ent, args.Used, args.User);
    }

    private bool TryPourSauce(EntityUid dish, EntityUid source, EntityUid user)
    {
        if (!HasComp<ADTDishComponent>(dish) || !CanAddToFood(dish) || !CanSendSolution(source))
            return false;

        if (!_solution.TryGetDrainableSolution(source, out var sourceSoln, out var sourceSolution))
            return false;

        var settings = Settings;
        if (!_dish.HasSauce(sourceSolution, settings))
            return false;

        var poured = _solution.SplitSolution(sourceSoln.Value, settings.SaucePourAmount);
        if (poured.Volume <= 0)
            return false;

        _dish.AddSauce(dish, poured, settings);
        AddToFood(dish, poured);
        _popup.PopupEntity(Loc.GetString("adt-dish-sauce-poured", ("dish", dish), ("source", source)), dish, user);
        return true;
    }
}
