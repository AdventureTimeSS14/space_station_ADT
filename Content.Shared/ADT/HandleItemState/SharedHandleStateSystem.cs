using Content.Shared.Interaction;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using System.Linq;
using System.Linq;
using Content.Shared.Flash.Components;
using Content.Shared.Charges.Components;
using Content.Shared.Charges.Systems;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Flash;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Tag;
using Content.Shared.Traits.Assorted;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.StatusEffect;
using Content.Shared.Examine;
using Robust.Shared.Audio;
using Robust.Shared.Random;
using System.Diagnostics.CodeAnalysis;
using Content.Shared.ActionBlocker;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Destructible;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;
using Content.Shared.Charges.Components;
using Content.Shared.Charges.Systems;
using Content.Shared.Flash;
using Content.Shared.StatusEffect;
using Content.Shared.Interaction.Events;
using Content.Shared.Mind.Components;

namespace Content.Shared.ADT.HandleItemState;

public abstract class SharedHandleItemStateSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HandleItemStateComponent, UseInHandEvent>(OnHandleItemActivate);
    }

    private void OnHandleItemActivate(EntityUid uid, HandleItemStateComponent scanner, UseInHandEvent args)
    {
        Log.Debug($"{uid} зашли в OnHandleItemActivate");
        if (args.Handled)
            return;

        scanner.Enabled = !scanner.Enabled;
        args.Handled = true;

        UpdateAppearance(uid, scanner);
        Log.Debug($"{uid} КОНЕЦ в OnHandleItemActivate");
    }

    private void UpdateAppearance(EntityUid uid, HandleItemStateComponent scanner)
    {
        Log.Debug($"{uid} в UpdateAppearance");
        if (TryComp<AppearanceComponent>(uid, out var appearance))
        {
            var visualState = scanner.Enabled ? HandleItemStateVisual.On : HandleItemStateVisual.Off;
            _appearance.SetData(uid, HandleItemStateVisual.Visual, visualState, appearance);
        }
    }
}

[Serializable, NetSerializable]
public enum HandleItemStateVisual : sbyte
{
    Visual,
    On,
    Off
}
