using Content.Shared.Body;
using Robust.Shared.GameObjects;

namespace Content.Shared.ADT.Novakid;

/// <summary>
/// Компанент свечения, обновляется на сервере при иницилизации и изменении внешности.
/// </summary>
public sealed class SharedNovakidGlowingSystem : EntitySystem
{
    [Dependency] private readonly SharedPointLightSystem _pointLight = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NovakidGlowingComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<NovakidGlowingComponent, ApplyOrganProfileDataEvent>(OnApplyProfile);
    }

    private void OnMapInit(Entity<NovakidGlowingComponent> ent, ref MapInitEvent args)
    {
        var light = _pointLight.EnsureLight(ent);
        _pointLight.SetEnabled(ent, true, light);
    }

    private void OnApplyProfile(Entity<NovakidGlowingComponent> ent, ref ApplyOrganProfileDataEvent args)
    {
        if (args.Base is { } baseProfile)
        {
            UpdateGlow(ent, baseProfile.SkinColor);
            return;
        }

        if (args.Profiles is null)
            return;

        foreach (var profile in args.Profiles.Values)
        {
            UpdateGlow(ent, profile.SkinColor);
            return;
        }
    }

    private void UpdateGlow(Entity<NovakidGlowingComponent> ent, Color color)
    {
        ent.Comp.GlowingColor = color;

        var light = _pointLight.EnsureLight(ent);
        _pointLight.SetColor(ent, color, light);
        _pointLight.SetEnabled(ent, true, light);
    }
}
