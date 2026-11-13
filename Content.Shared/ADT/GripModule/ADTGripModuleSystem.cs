using Content.Shared.Blocking.Components;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Standing;
using Content.Shared.Tools;
using Content.Shared.Tools.Systems;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.GripModule;

public sealed class ADTGripModuleSystem : EntitySystem
{
    private static readonly ProtoId<ToolQualityPrototype> RemovalQuality = "Screwing";

    [Dependency] private INetManager _net = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedToolSystem _tool = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTGripModuleComponent, AfterInteractEvent>(OnModuleAfterInteract);
        SubscribeLocalEvent<ADTGripModuleHolderComponent, InteractUsingEvent>(OnHolderInteractUsing);
        SubscribeLocalEvent<ADTGripModuleHolderComponent, ADTGripModuleRemoveDoAfterEvent>(OnRemoveDoAfter);
        SubscribeLocalEvent<ADTGripModuleHolderComponent, FellDownThrowAttemptEvent>(OnFellDownThrowAttempt);
        SubscribeLocalEvent<ADTGripModuleHolderComponent, ExaminedEvent>(OnExamined);
    }

    private void OnModuleAfterInteract(Entity<ADTGripModuleComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target is not { } target)
            return;

        if (!CanHoldModule(target))
            return;

        args.Handled = true;

        if (HasComp<ADTGripModuleHolderComponent>(target))
        {
            _popup.PopupClient(Loc.GetString("adt-grip-module-already-installed"), target, args.User);
            return;
        }

        var holder = EnsureComp<ADTGripModuleHolderComponent>(target);
        var container = _container.EnsureContainer<ContainerSlot>(target, holder.ContainerId);

        if (!_container.Insert(ent.Owner, container))
        {
            RemComp<ADTGripModuleHolderComponent>(target);
            return;
        }

        _audio.PlayPredicted(holder.InsertSound, target, args.User);
        _popup.PopupClient(Loc.GetString("adt-grip-module-inserted", ("item", target)), target, args.User);
    }

    private bool CanHoldModule(EntityUid target)
    {
        if (!HasComp<ItemComponent>(target) || HasComp<GunComponent>(target))
            return false;

        return HasComp<MeleeWeaponComponent>(target) || HasComp<BlockingComponent>(target);
    }

    private void OnHolderInteractUsing(Entity<ADTGripModuleHolderComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled || !_tool.HasQuality(args.Used, RemovalQuality))
            return;

        args.Handled = _tool.UseTool(args.Used,
            args.User,
            ent.Owner,
            ent.Comp.RemovalDelay,
            new[] { RemovalQuality },
            new ADTGripModuleRemoveDoAfterEvent());
    }

    private void OnRemoveDoAfter(Entity<ADTGripModuleHolderComponent> ent, ref ADTGripModuleRemoveDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;

        _audio.PlayPredicted(ent.Comp.RemoveSound, ent, args.User);
        _popup.PopupClient(Loc.GetString("adt-grip-module-removed"), ent, args.User);

        if (_net.IsServer && _container.TryGetContainer(ent, ent.Comp.ContainerId, out var container))
        {
            foreach (var module in container.ContainedEntities)
            {
                QueueDel(module);
            }
        }

        RemComp<ADTGripModuleHolderComponent>(ent);
    }

    private void OnFellDownThrowAttempt(Entity<ADTGripModuleHolderComponent> ent, ref FellDownThrowAttemptEvent args)
    {
        if (_mobState.IsIncapacitated(args.Thrower))
            return;

        args.Cancelled = true;
    }

    private void OnExamined(Entity<ADTGripModuleHolderComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("adt-grip-module-examine"));
    }
}
