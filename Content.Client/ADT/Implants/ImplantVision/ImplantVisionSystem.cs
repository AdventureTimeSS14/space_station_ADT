using System.Linq;
using Content.Shared.ADT.Implants.ImplantVision;
using Robust.Client.Graphics;
using Robust.Shared.GameStates;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Client.ADT.Implants.ImplantVision;

public sealed class ImplantVisionSystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlayManager = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private readonly Dictionary<ProtoId<ImplantVisionPrototype>, ImplantVisionOverlay> _overlays = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ImplantVisionComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<ImplantVisionComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<ImplantVisionComponent, AfterAutoHandleStateEvent>(OnState);
        SubscribeLocalEvent<ImplantVisionComponent, LocalPlayerAttachedEvent>(OnAttach);
        SubscribeLocalEvent<ImplantVisionComponent, LocalPlayerDetachedEvent>(OnDetach);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        Sync(null);
    }

    private void OnInit(Entity<ImplantVisionComponent> ent, ref ComponentInit args)
    {
        if (ent.Owner == _player.LocalEntity)
            Sync(ent.Comp);
    }

    private void OnShutdown(Entity<ImplantVisionComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Owner == _player.LocalEntity)
            Sync(null);
    }

    private void OnState(Entity<ImplantVisionComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (ent.Owner == _player.LocalEntity)
            Sync(ent.Comp);
    }

    private void OnAttach(Entity<ImplantVisionComponent> ent, ref LocalPlayerAttachedEvent args)
    {
        Sync(ent.Comp);
    }

    private void OnDetach(Entity<ImplantVisionComponent> ent, ref LocalPlayerDetachedEvent args)
    {
        Sync(null);
    }

    private void Sync(ImplantVisionComponent? comp)
    {
        foreach (var id in _overlays.Keys.ToList())
        {
            if (comp != null && comp.Effects.ContainsKey(id))
                continue;

            _overlayManager.RemoveOverlay(_overlays[id]);
            _overlays.Remove(id);
        }

        if (comp == null)
            return;

        foreach (var id in comp.Effects.Keys)
        {
            if (_overlays.ContainsKey(id) || !_prototype.TryIndex(id, out var proto))
                continue;

            var overlay = new ImplantVisionOverlay(proto);
            _overlays[id] = overlay;
            _overlayManager.AddOverlay(overlay);
        }
    }
}
