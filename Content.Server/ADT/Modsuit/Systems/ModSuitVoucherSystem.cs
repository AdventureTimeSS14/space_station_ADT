using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Content.Shared.ADT.ModSuits;
using Robust.Shared.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Audio.Systems;

namespace Content.Server.ADT.ModSuits;

public sealed class ModSuitVoucherSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    private static readonly ProtoId<TagPrototype> MODStation = "ModSuitStation";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ModSuitVoucherComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<ModSuitVoucherComponent, AfterInteractEvent>(OnAfterInteract);
    }

    private void OnUseInHand(Entity<ModSuitVoucherComponent> ent, ref UseInHandEvent args)
    {
        ent.Comp.Current = ent.Comp.Current == SuitType.MOD
            ? SuitType.Hard
            : SuitType.MOD;

        _appearance.SetData(ent, SuitVoucherVisuals.State, ent.Comp.Current);

        _popup.PopupEntity($"Voucher switched to {ent.Comp.Current}!", ent, args.User);
    }

    private void OnAfterInteract(Entity<ModSuitVoucherComponent> ent, ref AfterInteractEvent args)
    {
        if (!args.CanReach || args.Target is not { Valid: true } target)
            return;

        if (!_tag.HasTag(target, MODStation))
            return;

        var proto = ent.Comp.Current == SuitType.Hard
            ? ent.Comp.HardId
            : ent.Comp.ModId;

        if (_proto.HasIndex(proto))
        {
            var spawned = Spawn(proto, Transform(target).Coordinates);

            _popup.PopupEntity(
                $"Getting item {MetaData(spawned).EntityName}!",
                spawned,
                args.User);

            QueueDel(ent.Owner);
        }
    }
}