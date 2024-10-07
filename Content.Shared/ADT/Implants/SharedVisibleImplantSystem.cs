using Robust.Shared.Random;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Content.Shared.Movement.Systems;
using Content.Shared.Weapons.Melee;
using Content.Shared.Actions;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared.ADT.Implants;

public abstract class SharedVisibleImplantSystem : EntitySystem
{
    [Dependency] private readonly SharedEntityStorageSystem _storage = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly SharedActionsSystem _action = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();
    }
}
