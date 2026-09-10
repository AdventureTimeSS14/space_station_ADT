using Content.Shared.ADT.RPD.Components;
using Content.Shared.Charges.Components;
using Content.Shared.Charges.Systems;
using Content.Shared.Interaction;
using Content.Shared.Materials;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.RPD.Systems;

public sealed class RPDMaterialChargesSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedChargesSystem _charges = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedStackSystem _stack = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RPDMaterialChargesComponent, InteractUsingEvent>(OnInteractUsing);
    }

    private void OnInteractUsing(Entity<RPDMaterialChargesComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled || !_net.IsServer)
            return;

        if (!TryComp<LimitedChargesComponent>(ent, out var charges))
            return;

        var user = args.User;
        var needed = charges.MaxCharges - _charges.GetCurrentCharges((ent.Owner, charges, null));

        if (needed <= 0)
        {
            _popup.PopupClient(Loc.GetString("rpd-ammo-component-after-interact-full"), ent, user);
            return;
        }

        if (!TryGetChargeableMaterial(args.Used, ent.Comp, out var material, out var rate, out var sheetCount))
            return;

        var remainder = ent.Comp.Remainder.GetValueOrDefault(material);
        var neededUnits = needed * rate.Sheets - remainder;
        var sheetsToConsume = Math.Clamp((int) Math.Ceiling((double) neededUnits / rate.Charges), 0, sheetCount);

        if (sheetsToConsume <= 0)
            return;

        _stack.TryUse(args.Used, sheetsToConsume);

        var totalUnits = remainder + sheetsToConsume * rate.Charges;
        var chargesToAdd = Math.Min(needed, totalUnits / rate.Sheets);
        ent.Comp.Remainder[material] = totalUnits - chargesToAdd * rate.Sheets;
        Dirty(ent);

        _charges.AddCharges((ent.Owner, charges, null), chargesToAdd);
        args.Handled = true;

        _audio.PlayPredicted(ent.Comp.InsertSound, ent, user);
        _popup.PopupClient(Loc.GetString("rpd-ammo-component-after-interact-refilled"), ent, user);
    }

    private bool TryGetChargeableMaterial(EntityUid used, RPDMaterialChargesComponent comp, out ProtoId<MaterialPrototype> material, out RPDMaterialChargeRate rate, out int sheetCount)
    {
        material = default;
        rate = default!;
        sheetCount = 0;

        if (!TryComp<StackComponent>(used, out var stack) || stack.Count <= 0)
            return false;

        if (!TryComp<PhysicalCompositionComponent>(used, out var composition))
            return false;

        foreach (var (mat, _) in composition.MaterialComposition)
        {
            if (!comp.ChargeRates.TryGetValue(mat, out var chargeRate))
                continue;

            material = mat;
            rate = chargeRate;
            sheetCount = stack.Count;
            return true;
        }

        return false;
    }
}
