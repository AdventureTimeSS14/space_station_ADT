using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Content.Shared.ADT.ModSuits;
using Robust.Shared.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;
using Robust.Shared.Network;

namespace Content.Server.ADT.ModSuits;

public sealed class ModSuitVoucherSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;

    private static readonly ProtoId<TagPrototype> MODStation = "ModSuitStation";
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ModSuitVoucherComponent, AfterInteractEvent>(OnAfterInteract);
    }

    private void OnAfterInteract(Entity<ModSuitVoucherComponent> ent, ref AfterInteractEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (!args.CanReach || args.Target is not { Valid: true } target)
            return;

        if (!_tag.HasTag(target, MODStation))
            return;

        var proto = ent.Comp.Current switch
        {
            ModSuitType.MOD  => ent.Comp.ModId,
            ModSuitType.Hard => ent.Comp.HardId,
            => ent.Comp.ModId
        };

        if (_proto.HasIndex(proto))
        {
            var spawned = Spawn(proto, Transform(target).Coordinates);

            _popup.PopupEntity(
                $"getting-item {MetaData(spawned).EntityName}!",
                spawned,
                args.User);

            QueueDel(ent.Owner);
        }
    }
}
