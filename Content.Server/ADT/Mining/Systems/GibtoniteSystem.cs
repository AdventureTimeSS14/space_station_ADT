using Content.Shared.Damage;
using Content.Shared.ADT.Mining.Components;
using Content.Server.Explosion.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Mining.Components;
using Robust.Shared.Timing;
using Robust.Shared.GameObjects;
using Robust.Shared.Random;

namespace Content.Server.ADT.Mining.Systems;

public sealed class GibtoniteSystem : EntitySystem
{
    [Dependency] private readonly ExplosionSystem _explosion = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GibtoniteComponent, DamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<GibtoniteComponent, InteractUsingEvent>(OnItemInteract);
    }

    /// <summary>
    /// Мне НЕ НРАВИТСЯ как выглядит этот метод. Но ничего лучше я не придумал, так-что сойдет.
    /// </summary>
    private void OnDamageChanged(EntityUid uid, GibtoniteComponent comp, ref DamageChangedEvent args)
    {
        var triggered = false; // Нужно для проверки, был ли уже ударен гибтонит.

        if (!triggered && !comp.Active) // Проверка нужна, чтобы можно было вскопать гибтонит после дефьюза.
        {
            comp.Active = true;
            if (!comp.Extracted) // Нам НЕ НУЖНО это для вскопанного гибтонита.
            {
                triggered = true;
            }
            StartTimer(uid, comp);
            Dirty(uid, comp);
        }
        else
        {
            if (!comp.Extracted)
            {
                GetOre(uid, comp);
            }
        }
    }

    /// <summary>
    /// Просто таймер. Просто БУМ БУМ БУМ при его окончании.
    /// </summary>
    public void StartTimer(EntityUid uid, GibtoniteComponent comp)
    {
        if (!comp.Extracted) // Опять, нам НЕ НУЖНО это для вскопанного гибтонита.
        {
            var randomNumb = _random.Next(5); // Немного рандома не помешает.
            comp.ReactionMaxTime -= randomNumb;

            comp.ReactionTime = _timing.CurTime;
            comp.Active = true;
        }

        Timer.Spawn(TimeSpan.FromSeconds(comp.ReactionMaxTime), () =>
        {
            if (!EntityManager.EntityExists(uid) || !comp.Active)
                return;

            Explosion(uid, comp); // Boom boom boom boom. I want you in my room. We'll spend the night together. From now until forever.
        });
    }

    /// <summary>
    /// Чем позже остановили цепную реакцию - тем лучше будет взрыв.
    /// </summary>
    private void Explosion(EntityUid uid, GibtoniteComponent comp)
    {
        // Скейл достигает максимум модификатора в х2. Собственно, гибтонит который остановили в последнюю секунду - будет с силой в 450. 
        var scale = (float)comp.ReactionElapsedTime / comp.ReactionMaxTime + 1;
        var power = comp.MinIntensity * scale;

        var intensity = comp.Extracted
            ? Math.Clamp(power, comp.MinIntensity, comp.MaxIntensity) // на всякий.
            : comp.MinIntensity;

        if (comp.Extracted)
        {
            // Установка спрайта 
            comp.State = scale switch
            {
                >= 1.6f => GibtoniteState.OhFuck,
                >= 1.3f => GibtoniteState.Normal,
                _ => GibtoniteState.Nothing
            };
            Dirty(uid, comp);
        }

        if (!comp.Active)
            return;

        _explosion.QueueExplosion(
            uid,
            "DemolitionCharge",
            totalIntensity: intensity,
            slope: 2.5f,
            maxTileIntensity: 10f,
            canCreateVacuum: true
        );

        if (EntityManager.EntityExists(uid))
            QueueDel(uid);
    }

    /// <summary>
    /// Логика дефьюза и записи времени до взрыва.
    /// </summary>
    private void OnItemInteract(EntityUid uid, GibtoniteComponent comp, ref InteractUsingEvent args)
    {
        if (!HasComp<MiningScannerComponent>(args.Used) && comp.Extracted)
            return;

        comp.Active = false;
        comp.ReactionElapsedTime = (float)(_timing.CurTime - comp.ReactionTime).TotalSeconds; // Считаем, сколько секунд осталось до взрыва.
        Dirty(uid, comp);
    }

    /// <summary>
    /// GIVE ME MY FUCKIN ORE.
    /// </summary>
    private void GetOre(EntityUid uid, GibtoniteComponent comp)
    {
        var ore = Spawn(comp.OrePrototype, _transform.GetMapCoordinates(uid));
        if (TryComp<GibtoniteComponent>(ore, out var oreComp))
        {
            oreComp.Extracted = true;
            oreComp.ReactionElapsedTime = comp.ReactionElapsedTime; // Нужно для модификатора взрыва у выкопанного гибтонита.
            oreComp.ReactionMaxTime -= comp.ReactionElapsedTime - 1; // Устанавливаем макс. время для выкопанного гибтонита. -1 Нужно для более удобной игры шахетерам aka подушка безопасности.
            var gibtoniteSystem = EntityManager.EntitySysManager.GetEntitySystem<GibtoniteSystem>();
            gibtoniteSystem.Explosion(ore, oreComp);
        }

        QueueDel(uid); // Типа вскопали лмао.
    }
}