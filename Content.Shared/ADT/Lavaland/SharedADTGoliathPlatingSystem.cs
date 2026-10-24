using Content.Shared.ADT.Lavaland.Components;
using Content.Shared.Armor;
using Content.Shared.Damage;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Stacks;

namespace Content.Shared.ADT.Lavaland;

public abstract class SharedADTGoliathPlatingSystem : EntitySystem
{
    [Dependency] private SharedArmorSystem _armor = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedStackSystem _stack = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTGoliathPlatableComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<ADTGoliathPlatableComponent, ExaminedEvent>(OnExamined);
    }

    private void OnInteractUsing(Entity<ADTGoliathPlatableComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled || !IsPlatable(ent) || !TryComp<StackComponent>(args.Used, out var stack) || stack.StackTypeId != ent.Comp.Stack)
            return;

        args.Handled = true;

        if (!CanPlate(ent, out var armor))
        {
            _popup.PopupClient(Loc.GetString("adt-goliath-plating-max"), ent.Owner, args.User);
            return;
        }

        if (!_stack.TryUse((args.Used, stack), 1))
            return;

        var modifiers = new DamageModifierSet
        {
            Coefficients = new(armor.Modifiers.Coefficients),
            FlatReductions = new(armor.Modifiers.FlatReductions),
        };

        foreach (var type in ent.Comp.Types)
        {
            var current = modifiers.Coefficients.GetValueOrDefault(type, 1f);
            modifiers.Coefficients[type] = MathF.Max(current - ent.Comp.Step, ent.Comp.MinCoefficient);
        }

        _armor.SetArmorModifiers(ent.Owner, modifiers, armor);

        ent.Comp.Plates++;
        Dirty(ent);
        UpdateVisuals(ent);

        _popup.PopupClient(Loc.GetString("adt-goliath-plating-done", ("item", ent.Owner)), ent.Owner, args.User);
    }

    private void OnExamined(Entity<ADTGoliathPlatableComponent> ent, ref ExaminedEvent args)
    {
        if (!IsPlatable(ent))
            return;

        var message = CanPlate(ent, out _)
            ? "adt-goliath-plating-examine"
            : "adt-goliath-plating-examine-max";

        args.PushMarkup(Loc.GetString(message));
    }

    protected virtual void UpdateVisuals(Entity<ADTGoliathPlatableComponent> ent)
    {
    }

    private static bool IsPlatable(Entity<ADTGoliathPlatableComponent> ent)
    {
        return ent.Comp.Types.Count > 0;
    }

    private bool CanPlate(Entity<ADTGoliathPlatableComponent> ent, out ArmorComponent armor)
    {
        if (!TryComp(ent.Owner, out armor!) || ent.Comp.MaxPlates > 0 && ent.Comp.Plates >= ent.Comp.MaxPlates)
            return false;

        foreach (var type in ent.Comp.Types)
        {
            if (armor.Modifiers.Coefficients.GetValueOrDefault(type, 1f) > ent.Comp.MinCoefficient)
                return true;
        }

        return false;
    }
}
