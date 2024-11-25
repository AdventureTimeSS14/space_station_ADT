using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Examine;
using Content.Shared.Inventory;
using Content.Shared.Silicons.Borgs;
using Content.Shared.Verbs;
using Robust.Shared.Utility;

namespace Content.Shared.Armor;

/// <summary>
///     This handles logic relating to <see cref="ArmorComponent" />
/// </summary>
public abstract class SharedArmorSystem : EntitySystem
{
    [Dependency] private readonly ExamineSystemShared _examine = default!;

    /// <inheritdoc />
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ArmorComponent, InventoryRelayedEvent<DamageModifyEvent>>(OnDamageModify);
        SubscribeLocalEvent<ArmorComponent, BorgModuleRelayedEvent<DamageModifyEvent>>(OnBorgDamageModify);
        SubscribeLocalEvent<ArmorComponent, GetVerbsEvent<ExamineVerb>>(OnArmorVerbExamine);
        SubscribeLocalEvent<ArmorComponent, InventoryRelayedEvent<StaminaDamageModifyEvent>>(OnStaminaDamageModify);    // ADT Stunmeta fix
    }

    private void OnDamageModify(EntityUid uid, ArmorComponent component, InventoryRelayedEvent<DamageModifyEvent> args)
    {
        args.Args.Damage = DamageSpecifier.ApplyModifierSet(args.Args.Damage, component.Modifiers);
    }

    private void OnBorgDamageModify(EntityUid uid, ArmorComponent component,
        ref BorgModuleRelayedEvent<DamageModifyEvent> args)
    {
        args.Args.Damage = DamageSpecifier.ApplyModifierSet(args.Args.Damage, component.Modifiers);
    }

    private void OnArmorVerbExamine(EntityUid uid, ArmorComponent component, GetVerbsEvent<ExamineVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        var examineMarkup = GetArmorExamine(component.Modifiers, component.StaminaModifier);    // ADT Stunmeta fix

        var ev = new ArmorExamineEvent(examineMarkup);
        RaiseLocalEvent(uid, ref ev);

        _examine.AddDetailedExamineVerb(args, component, examineMarkup,
            Loc.GetString("armor-examinable-verb-text"), "/Textures/Interface/VerbIcons/dot.svg.192dpi.png",
            Loc.GetString("armor-examinable-verb-message"));
    }

    private FormattedMessage GetArmorExamine(DamageModifierSet armorModifiers, float staminaModifier)   // ADT Stunmeta fix
    {
        var msg = new FormattedMessage();
        msg.AddMarkupOrThrow(Loc.GetString("armor-examine"));

        foreach (var coefficientArmor in armorModifiers.Coefficients)
        {
            msg.PushNewline();

            var armorType = Loc.GetString("armor-damage-type-" + coefficientArmor.Key.ToLower());
            var coefficient = MathF.Round((1f - coefficientArmor.Value) * 100, 1);  // ADT tweak
            bool decrease = coefficient >= 0;                            // ADT tweak

            msg.AddMarkupOrThrow(Loc.GetString("armor-coefficient-value" + (decrease ? String.Empty : "-increase"), // ADT tweak
                ("type", armorType),
                ("value", Math.Abs(coefficient))    // ADT tweak
            ));
        }

        foreach (var flatArmor in armorModifiers.FlatReduction)
        {
            msg.PushNewline();

            var armorType = Loc.GetString("armor-damage-type-" + flatArmor.Key.ToLower());
            var coefficient = flatArmor.Value;      // ADT tweak
            bool decrease = flatArmor.Value >= 0;   // ADT tweak

            msg.AddMarkupOrThrow(Loc.GetString("armor-reduction-value" + (decrease ? String.Empty : "-increase"),   // ADT tweak
                ("type", armorType),
                ("value", Math.Abs(coefficient))    // ADT tweak
            ));
        }

        // ADT Stunmeta fix start
        if (staminaModifier != 1)
        {
            msg.PushNewline();
            msg.AddMarkupOrThrow(Loc.GetString("armor-stamina-protection-value", ("value", MathF.Round((1f - staminaModifier) * 100, 1))));
        }

        // ADT Stunmeta fix end

        return msg;
    }

    // ADT Stunmeta fix start
    private void OnStaminaDamageModify(EntityUid uid, ArmorComponent component, InventoryRelayedEvent<StaminaDamageModifyEvent> args)
    {
        args.Args.Damage *= component.StaminaModifier;
    }
    // ADT Stunmeta fix end
}
