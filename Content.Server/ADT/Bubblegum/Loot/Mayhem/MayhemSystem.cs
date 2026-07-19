using Content.Server.Chat.Systems;
using Content.Shared.ADT.Bubblegum.Loot;
using Content.Shared.Humanoid;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Bubblegum.Loot;

public sealed class MayhemSystem : EntitySystem
{
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MayhemComponent, UseInHandEvent>(OnUseInHand);
    }

    private void OnUseInHand(Entity<MayhemComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        var coords = _transform.GetMapCoordinates(args.User);
        var targets = new HashSet<Entity<HumanoidProfileComponent>>();
        _lookup.GetEntitiesInRange(coords, ent.Comp.Range, targets);

        var applyAt = _timing.CurTime + ent.Comp.EffectDelay;
        foreach (var target in targets)
        {
            EnsureComp<BloodFrenzyPendingComponent>(target).ApplyAt = applyAt;
        }

        _popup.PopupEntity(Loc.GetString("adt-mayhem-shatter"), args.User, args.User);
        _audio.PlayPvs(ent.Comp.BreakSound, args.User);

        _chat.DispatchGlobalAnnouncement(
            Loc.GetString("adt-blood-frenzy-announcement"),
            Loc.GetString("adt-blood-frenzy-announcer"),
            true,
            new SoundPathSpecifier("/Audio/ADT/Phantom/Sounds/freedom-deathmatch.ogg"),
            Color.FromHex("#FF3030"));

        QueueDel(ent.Owner);
    }
}
