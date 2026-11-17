using Content.Shared.ADT.Chaplain.Components;
using Content.Shared.ADT.Chaplain.Systems;
using Content.Shared.StatusIcon.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Prototypes;

namespace Content.Client.ADT.Chaplain;

public sealed class ChaplainSystem : SharedChaplainSystem
{
    [Dependency] private IPrototypeManager _proto = default!;

    private static readonly ProtoId<ShaderPrototype> HolyShieldShader = "ADTHolyShieldShader";

    private ShaderInstance? _shieldShader;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChaplainComponent, GetStatusIconsEvent>(OnChaplainStatusIcons);
        SubscribeLocalEvent<ChaplainFollowerComponent, GetStatusIconsEvent>(OnFollowerStatusIcons);
        SubscribeLocalEvent<HolyShieldComponent, ComponentStartup>(OnShieldStartup);
        SubscribeLocalEvent<HolyShieldComponent, ComponentShutdown>(OnShieldShutdown);
    }

    private void OnChaplainStatusIcons(Entity<ChaplainComponent> ent, ref GetStatusIconsEvent args)
    {
        if (_proto.TryIndex(ent.Comp.StatusIcon, out var icon))
            args.StatusIcons.Add(icon);
    }

    private void OnFollowerStatusIcons(Entity<ChaplainFollowerComponent> ent, ref GetStatusIconsEvent args)
    {
        if (_proto.TryIndex(ent.Comp.StatusIcon, out var icon))
            args.StatusIcons.Add(icon);
    }

    private void OnShieldStartup(Entity<HolyShieldComponent> ent, ref ComponentStartup args)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite) || sprite.PostShader != null)
            return;

        _shieldShader ??= _proto.Index(HolyShieldShader).InstanceUnique();
        sprite.PostShader = _shieldShader;
    }

    private void OnShieldShutdown(Entity<HolyShieldComponent> ent, ref ComponentShutdown args)
    {
        if (TryComp<SpriteComponent>(ent, out var sprite) && sprite.PostShader == _shieldShader)
            sprite.PostShader = null;
    }
}
