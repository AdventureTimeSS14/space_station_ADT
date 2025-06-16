using Content.Server.Lightning;
using Content.Shared.ADT.Research.Components;
using Content.Server.Research.Components;
using Content.Shared.Research.Components;

namespace Content.Server.ADT.Research.System;

/// <summary>
/// Генерируем очки исследований про попадании молнии.
/// </summary>
public sealed class ResearchTeslaCoilSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ResearchTeslaCoilComponent, HitByLightningEvent>(OnHitByLightning);
        SubscribeLocalEvent<ResearchTeslaCoilComponent, ResearchServerGetPointsPerSecondEvent>(OnGetPointsPerSecond);
    }

    private void OnHitByLightning(Entity<ResearchTeslaCoilComponent> coil, ref HitByLightningEvent args)
    {
        coil.Comp.HitByLightning = true;
    }

    // TODO: Сделать аналог молний СМа для теслы/аномалии и других источников молнии, чтобы кол-во очков можно было скейлить от силы молнии.
    private void OnGetPointsPerSecond(Entity<ResearchTeslaCoilComponent> coil, ref ResearchServerGetPointsPerSecondEvent args)
    {
        if (!coil.Comp.HitByLightning)
            return;

        args.Points += coil.Comp.Points;
        coil.Comp.HitByLightning = false;
    }
}