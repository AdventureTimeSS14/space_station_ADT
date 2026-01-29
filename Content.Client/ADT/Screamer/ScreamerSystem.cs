using Content.Shared.ADT.Screamer;
using Robust.Client.Audio;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Audio;
using Robust.Shared.Player;

namespace Content.Client.ADT.Screamer;

public sealed class ScreamerSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IOverlayManager _overlayMan = default!;
    [Dependency] private readonly AudioSystem _audio = default!;

    private ScreamerOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        _overlay = new();

        SubscribeNetworkEvent<DoScreamerMessage>(OnScreamerMessage);

        SubscribeLocalEvent<ScreamersComponent, ComponentInit>(OnScreamersInit);
        SubscribeLocalEvent<ScreamersComponent, ComponentShutdown>(OnScreamersShutdown);
        SubscribeLocalEvent<ScreamersComponent, LocalPlayerAttachedEvent>(OnScreamersAttach);
        SubscribeLocalEvent<ScreamersComponent, LocalPlayerDetachedEvent>(OnScreamersDetach);

    }

    private void OnScreamerMessage(DoScreamerMessage args)
    {
        if (!HasComp<ScreamersComponent>(_player.LocalEntity))
            return;

        if (args.Sound != null)
            _audio.PlayGlobal(new SoundPathSpecifier(args.Sound), _player.LocalEntity.Value);

        var ent = Spawn(args.ProtoId);
        _overlay.AddScreamer(ent, args.Offset, args.Duration, args.Alpha, args.FadeIn, args.FadeOut);
    }

    private void OnScreamersInit(Entity<ScreamersComponent> ent, ref ComponentInit args)
    {
        if (_player.LocalEntity != ent.Owner)
            return;

        _overlay.Clear();
        _overlayMan.AddOverlay(_overlay);
    }

    private void OnScreamersShutdown(Entity<ScreamersComponent> ent, ref ComponentShutdown args)
    {
        if (_player.LocalEntity != ent.Owner)
            return;

        _overlay.Clear();
        _overlayMan.RemoveOverlay(_overlay);
    }

    private void OnScreamersAttach(Entity<ScreamersComponent> ent, ref LocalPlayerAttachedEvent args)
    {
        _overlay.Clear();
        _overlayMan.AddOverlay(_overlay);
    }

    private void OnScreamersDetach(Entity<ScreamersComponent> ent, ref LocalPlayerDetachedEvent args)
    {
        _overlay.Clear();
        _overlayMan.RemoveOverlay(_overlay);
    }
}
