using Content.Shared.Inventory;
using Content.Shared.Clothing;
using Content.Server.Hands.Systems;
using Robust.Shared.Prototypes;
using Content.Server.Body.Systems;
using Robust.Shared.Audio.Systems;
using Content.Server.Emp;
using Content.Shared.DoAfter;
using Content.Server.Fluids.EntitySystems;
using Robust.Shared.Map;
using Robust.Shared.Random;
using Robust.Shared.Audio;
using Content.Server.Weapons.Ranged.Systems;
using Robust.Server.GameObjects;
using Content.Shared.Throwing;
using Content.Shared.Inventory.Events;
using Content.Server.Actions;

namespace Content.Server.Clothing.EntitySystems;

public sealed partial class JumpbootsSystem : SharedJumpbootsSystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly HandsSystem _handsSystem = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly BloodstreamSystem _bloodstreamSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly EmpSystem _emp = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly PuddleSystem _puddle = default!;
    [Dependency] private readonly IComponentFactory _compFact = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly BodySystem _bodySystem = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly PhysicsSystem _physics = default!;
    [Dependency] private readonly GunSystem _gunSystem = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly ActionsSystem _action = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<JumpbootsComponent, GotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<JumpbootsComponent, GotUnequippedEvent>(OnGotUnequipped);

        SubscribeLocalEvent<JumpbootsComponent, JumpbootsActionEvent>(OnJump);
    }

    private void OnGotUnequipped(EntityUid uid, JumpbootsComponent component, GotUnequippedEvent args)
    {
        if (args.Slot == "shoes")
        {
            _action.AddAction(uid, ref component.ActionEntity, component.Action);
        }
    }

    private void OnGotEquipped(EntityUid uid, JumpbootsComponent component, GotEquippedEvent args)
    {
        if (args.Slot == "shoes")
        {
        }
    }
    private void OnJump(EntityUid uid, JumpbootsComponent component, JumpbootsActionEvent args)
    {
        if (args.Handled)
            return;

        var transform = Transform(uid);

        if (transform.MapID != args.Target.GetMapId(EntityManager))
            return;

        _throwing.TryThrow(args.Performer, args.Target, component.Strength);
        _audio.PlayPvs(args.Sound, uid, AudioParams.Default.WithVolume(component.Volume));
        args.Handled = true;
    }
}
