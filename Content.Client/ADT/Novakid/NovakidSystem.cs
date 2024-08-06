using Content.Shared.ADT.Novakid;
using Robust.Client.GameObjects;

namespace Content.Client.ADT.Novakid;
public sealed partial class NovakidSystem : EntitySystem
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly PointLightSystem _lights = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NovakidGlowingComponent, ComponentInit>(OnEntityInit);
    }
    /// <summary>
    /// Makes Novakid to glow with color of his skin — layer number 0 of SpriteComponent.
    /// </summary>
    /// <param name="uid">Novakid</param>
    /// <param name="component">Компонент отвечающий за свечение новакида. Вообще-то говоря он нужен пока только для того, чтобы не пришлось перебирать сущности в поисках новакида.</param>
    /// <param name="args"></param>
    private void OnEntityInit(EntityUid uid, NovakidGlowingComponent component, ComponentInit args)
    {
        _entityManager.GetComponent<SpriteComponent>(uid).TryGetLayer(0, out var layer);

        component.GlowingColor = _entityManager.GetComponent<SpriteComponent>(uid).Color;
        if (layer == null) return;
        _lights.SetColor(uid, layer.Color);
        //_lights.SetEnergy(uid, 0.6f);
        DirtyEntity(uid);
    }
}
