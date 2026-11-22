using Content.Shared.ADT.GodEye;
using Robust.Client.Player;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Client.ADT.GodEye;

public sealed class ADTGodEyeVisionSystem : EntitySystem
{
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    private static readonly EntProtoId LightProto = "ADTEffectGodEyeLight";

    private EntityUid? _light;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTGodEyeVisionComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<ADTGodEyeVisionComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<ADTGodEyeVisionComponent, LocalPlayerAttachedEvent>(OnAttached);
        SubscribeLocalEvent<ADTGodEyeVisionComponent, LocalPlayerDetachedEvent>(OnDetached);
    }

    private void OnStartup(Entity<ADTGodEyeVisionComponent> ent, ref ComponentStartup args)
    {
        if (ent.Owner == _player.LocalEntity)
            AddLight(ent);
    }

    private void OnShutdown(Entity<ADTGodEyeVisionComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Owner == _player.LocalEntity)
            RemoveLight();
    }

    private void OnAttached(Entity<ADTGodEyeVisionComponent> ent, ref LocalPlayerAttachedEvent args)
    {
        AddLight(ent);
    }

    private void OnDetached(Entity<ADTGodEyeVisionComponent> ent, ref LocalPlayerDetachedEvent args)
    {
        RemoveLight();
    }

    private void AddLight(EntityUid uid)
    {
        RemoveLight();

        _light = Spawn(LightProto, Transform(uid).Coordinates);
        _transform.SetParent(_light.Value, uid);
    }

    private void RemoveLight()
    {
        if (_light is { } light && !TerminatingOrDeleted(light))
            Del(light);

        _light = null;
    }
}
