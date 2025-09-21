using Content.Shared.Damage;
using Content.Shared.ADT.Salvage.Components;
using Content.Server.Explosion.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Components;
using Content.Shared.Mining.Components;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Salvage.Systems;

public sealed class GibtoniteSystem : EntitySystem
{
    [Dependency] private readonly ExplosionSystem _explosion = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GibtoniteComponent, DamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<GibtoniteComponent, InteractUsingEvent>(OnItemInteract);
    }

    /// <summary>
    /// Я хз как вынести нормально модификацию данных взрыва, так-что всё будет захардкожено лмао.
    /// </summary>
    private void CalculatePower(EntityUid uid, GibtoniteComponent comp)
    {
        if (!comp.Extracted)
        {
            _explosion.QueueExplosion(
                uid,
                "DemolitionCharge",
                totalIntensity: 0.1f,
                slope: 1f,
                maxTileIntensity: 5f,
                tileBreakScale: 2f,
                maxTileBreak: 5,
                canCreateVacuum: true
            );
        }
    }

    /// <summary>
    /// При получении урона по сущности - врубаем таймер. 
    /// </summary>
    private void OnDamageChanged(EntityUid uid, GibtoniteComponent comp, ref DamageChangedEvent args)
    {
        if (comp.Extracted)
            return;

        comp.Active = true;
        comp.ReactionTime = _timing.CurTime;

        Timer.Spawn(TimeSpan.FromSeconds(comp.MaxReactionTime), () => // ТАЙМЕРУ КОНЕЦ ДЕЛАЕМ БУМ БУМ БУМ
        {
            if (comp.Active)
            {
                CalculatePower(uid, comp);
            }
        });
    }

    private void OnItemInteract(EntityUid uid, GibtoniteComponent comp, ref InteractUsingEvent args)
    {
        if (HasComp<MiningScannerComponent>(args.Used) && comp.Active) // Я не помню, зачем добавил проверку на Active. Но это точно что-то важное, по этому нужно будет вспомнить.
        {
            comp.Active = false;
            var elapsed = (_timing.CurTime - comp.ReactionTime).TotalSeconds;

            comp.Extracted = true;
        }
    }
}