using Content.Server.Administration;
using Content.Server.Audio;
using Content.Server.Emp;
using Content.Server.Popups;
using Content.Server.Power.Components;
using Content.Shared.Audio;
using Content.Shared.Construction;
using Content.Shared.Damage;
using Content.Shared.Humanoid;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Toggleable;
using Robust.Server.Audio;
using Robust.Server.Containers;
using Robust.Server.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Terraformer;

public sealed partial class TerraformerSystem : EntitySystem
{
    [Dependency] private readonly ContainerSystem _container = default!;
    [Dependency] private readonly QuickDialogSystem _quickDialog = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly AmbientSoundSystem _ambient = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly EmpSystem _emp = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TerraformerComponent, AfterInteractUsingEvent>(OnInteract);
    }

    private void OnInteract(EntityUid uid, TerraformerComponent component, AfterInteractUsingEvent args)
    {
        if (args.Handled || !args.CanReach || args.Used != args.User)
            return;

        if (!IsDiskInserted(uid))
            return;

        if (!_player.TryGetSessionByEntity(args.User, out var session))
            return;

        _quickDialog.OpenDialog<string>(session, "Ожидание кода", "Код", arg =>
        {
            if (arg != component.ActivationCode)
            {
                _popup.PopupEntity("Консоль сигнализирует о том, что код неверен.", uid, PopupType.SmallCaution);
                _audio.PlayLocal(component.DenySound, uid, null);
                return;
            }

            _popup.PopupEntity("Механизм пришёл в действие.", uid, PopupType.Large);
            _ambient.SetAmbience(uid, true);
            _appearance.SetData(uid, ToggleVisuals.Toggled, true);
            Timer.Spawn(TimeSpan.FromSeconds(60), () =>
            {
                DoEffect(uid);
                _ambient.SetAmbience(uid, false);
            });
        });
    }

    private void DoEffect(EntityUid uid)
    {
        _appearance.SetData(uid, ToggleVisuals.Toggled, false);
        _emp.EmpPulse(Transform(uid).Coordinates, 100f, 1000f, 10f);

        var receivers = _lookup.GetEntitiesInRange<ApcPowerReceiverComponent>(Transform(uid).Coordinates, 100f);
        foreach (var ent in receivers)
        {
            if (ent.Owner == uid)
                continue;

            _damage.TryChangeDamage(ent.Owner, new() { DamageDict = new() { { "Structural", 1000 } } }, true, true);
        }

        var generators = _lookup.GetEntitiesInRange<PowerSupplierComponent>(Transform(uid).Coordinates, 100f);
        foreach (var ent in generators)
        {
            if (ent.Owner == uid)
                continue;

            _damage.TryChangeDamage(ent.Owner, new() { DamageDict = new() { { "Structural", 1000 } } }, true, true);
        }

        var batteries = _lookup.GetEntitiesInRange<BatteryComponent>(Transform(uid).Coordinates, 100f);
        foreach (var ent in batteries)
        {
            if (ent.Owner == uid)
                continue;
            if (HasComp<HumanoidAppearanceComponent>(ent.Owner))
                continue;

            QueueDel(ent.Owner);
        }
    }

    private bool IsDiskInserted(EntityUid uid)
    {
        if (!_container.TryGetContainer(uid, TerraformerComponent.ContainerId, out var container))
            return false;

        return container.ContainedEntities.Count > 0;
    }
}
