using System.Linq;
using Content.Server.Interaction;
using Content.Server.Mech.Equipment.Components;
using Content.Server.Mech.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Mech;
using Content.Shared.Mech.Components;
using Content.Shared.Mech.Equipment.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Wall;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Content.Shared.Damage;
using Content.Server.ADT.Mech.Equipment.Components;

namespace Content.Server.ADT.Mech.Equipment.EntitySystems;

public sealed class MechArmorSystem : EntitySystem
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<MechArmorComponent, MechEquipmentInsertedEvent>(OnEquipmentInstalled);
        SubscribeLocalEvent<MechArmorComponent, MechEquipmentRemovedEvent>(OnEquipmentRemoved);
    }

    private void OnEquipmentInstalled(EntityUid uid, MechArmorComponent component, ref MechEquipmentInsertedEvent args)
    {
        if (!TryComp<MechComponent>(args.Mech, out var mech))
            return;
        component.OriginalModifiers = mech.Modifiers;
        mech.Modifiers = component.Modifiers;
    }

    private void OnEquipmentRemoved(EntityUid uid, MechArmorComponent component, ref MechEquipmentRemovedEvent args)
    {
        if (!TryComp<MechComponent>(args.Mech, out var mech))
            return;
        component.OriginalModifiers = null;
        mech.Modifiers = component.OriginalModifiers;
    }

    private DamageModifierSet SumModifierSets(DamageModifierSet modifier1, DamageModifierSet modifier2)
    {
        var modifier3 = modifier2;

        foreach (var item in modifier1.FlatReduction)
        {
            if (modifier3.FlatReduction.TryGetValue(item.Key, out _))
            {
                modifier3.FlatReduction[item.Key] += item.Value;
            }
        }
        foreach (var item in modifier1.Coefficients)
        {
            if (modifier3.Coefficients.TryGetValue(item.Key, out _))
            {
                modifier3.Coefficients[item.Key] += item.Value;
            }
        }
        return modifier3;
    }
    private DamageModifierSet MinodifierSets(DamageModifierSet modifier1, DamageModifierSet modifier2)
    {
        var modifier3 = modifier2;

        foreach (var item in modifier1.FlatReduction)
        {
            if (modifier3.FlatReduction.TryGetValue(item.Key, out _) && modifier3.Coefficients[item.Key] >= item.Value)
            {
                modifier3.FlatReduction[item.Key] -= item.Value;
            }
        }
        foreach (var item in modifier1.Coefficients)
        {
            if (modifier3.Coefficients.TryGetValue(item.Key, out _) && modifier3.Coefficients[item.Key] >= item.Value)
            {
                modifier3.Coefficients[item.Key] -= item.Value;
            }
        }
        return modifier3;
    }
}
