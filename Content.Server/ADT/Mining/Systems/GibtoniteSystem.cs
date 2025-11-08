using Content.Shared.Damage;
using Content.Shared.ADT.Mining.Components;
using Content.Server.Explosion.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Mining.Components;
using Robust.Shared.Timing;
using Robust.Shared.GameObjects;
using Robust.Shared.Random;
using Content.Shared.Tag;
using Content.Shared.Trigger;
using Content.Server.Popups;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;
using Content.Server.Kitchen.Components;
using Content.Server.Gatherable.Components;

namespace Content.Server.ADT.Mining.Systems;

public sealed class GibtoniteSystem : EntitySystem
{
    [Dependency] private readonly ExplosionSystem _explosion = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly TagSystem _tag = default!;

    private static readonly ProtoId<TagPrototype> PlasticKnife = "Plastic";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GibtoniteComponent, DamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<GibtoniteComponent, InteractUsingEvent>(OnItemInteract);
        SubscribeLocalEvent<GibtoniteComponent, MapInitEvent>(OnStartup);
    }

    private void OnStartup(EntityUid uid, GibtoniteComponent comp, MapInitEvent args)
    {
        if (!comp.Extracted && HasComp<GatherableComponent>(uid))
            RemComp<GatherableComponent>(uid);
        RandomTimer(comp);
    }

    /// <summary>
    /// Генерация рандомного времени для гибтонита.
    /// </summary>
    private void RandomTimer(GibtoniteComponent comp)
    {
        if (!comp.Extracted) // Для руды нам это НЕ НУЖНО.
        {
            var randomNumb = _random.Next(2);
            comp.ReactionMaxTime -= randomNumb;
        }

        comp.ReactionTime = _timing.CurTime;
        comp.ReactionElapsedTime = 0f;
    }

    /// <summary>
    /// Просто таймер. Просто БУМ БУМ БУМ при его окончании.
    /// </summary>
    public override void Update(float frameTime)
    {
        foreach (var comp in EntityManager.EntityQuery<GibtoniteComponent>())
        {
            if (!comp.Active)
                continue;

            // Считаем, сколько времени прошло
            comp.ReactionElapsedTime = (float)(_timing.CurTime - comp.ReactionTime).TotalSeconds;

            // Взорвать гибтонит, если время истекло
            if (comp.ReactionElapsedTime >= comp.ReactionMaxTime)
            {
                if (EntityManager.EntityExists(comp.Owner))
                    Explosion(comp.Owner, comp);
            }
        }
    }

    private void OnDamageChanged(EntityUid uid, GibtoniteComponent comp, ref DamageChangedEvent args)
    {
        if (comp.Active) // Если при ударе гибтонит уже был активен - моментальный BOOM BOOM BOOM
        {
            Explosion(uid, comp);
            return;
        }

        Activate(uid, comp);
    }

    private void Activate(EntityUid uid, GibtoniteComponent comp)
    {
        if (comp.Triggered) // Если камень до этого уже ударили - дропаем руду.
        {
            if (!comp.Active && !comp.Extracted && !(comp.ReactionElapsedTime >= comp.ReactionMaxTime))
            {
                GetOre(uid, comp);
                return;
            }
        }
        else
        {
            if (!comp.Extracted) // Запись того, что камень ударили впервые.
                comp.Triggered = true;

            _popup.PopupEntity(Loc.GetString("gibtonit-get-damage"), uid, PopupType.LargeCaution);

            // Активируем таймер
            comp.Active = true;
            comp.ReactionTime = _timing.CurTime;

            UpdateAppearance(uid, comp);
        }
    }

    /// <summary>
    /// Обработка спрайтов для вскопанного гибтонита.
    /// </summary>
    private void UpdateAppearance(EntityUid uid, GibtoniteComponent comp, AppearanceComponent? appearance = null)
    {
        if (!Resolve(uid, ref appearance, false))
            return;

        _appearance.SetData(uid, GibtoniteVisuals.Active, comp.Active, appearance);
        _appearance.SetData(uid, GibtoniteVisuals.State, comp.State, appearance);
    }

    /// <summary>
    /// Чем позже остановили цепную реакцию - тем лучше будет взрыв.
    /// </summary>
    private void Explosion(EntityUid uid, GibtoniteComponent comp)
    {
        if (!comp.Active) // Если неактивен - не взрываем
            return;

        comp.Active = false;

        // Скейл достигает максимум модификатора в х2. 
        var scale = (float)comp.ReactionElapsedTime / comp.ReactionMaxTime + 1;
        var power = comp.MinIntensity * scale;

        var intensity = comp.Extracted
            ? Math.Clamp(power, comp.MinIntensity, comp.MaxIntensity) // на всякий
            : comp.MaxIntensity;

        if (comp.Extracted)
        {
            // Установка спрайта 
            comp.State = scale switch
            {
                >= 1.6f => GibtoniteState.OhFuck,
                >= 1.3f => GibtoniteState.Normal,
                _ => GibtoniteState.Nothing
            };
            UpdateAppearance(uid, comp);
        }

        _explosion.QueueExplosion(
            uid,
            "DemolitionCharge",
            totalIntensity: intensity,
            slope: 2.5f,
            maxTileIntensity: 10f,
            canCreateVacuum: true
        );

        QueueDel(uid);
    }

    /// <summary>
    /// Логика дефьюза и записи времени до взрыва.
    /// </summary>
    private void OnItemInteract(EntityUid uid, GibtoniteComponent comp, ref InteractUsingEvent args)
    {
        if (HasComp<MiningScannerComponent>(args.Used))
        {
            if (comp.Extracted)
                return;

            comp.Active = false; // Деактивируем - это остановит взрыв
            comp.ReactionElapsedTime = (float)(_timing.CurTime - comp.ReactionTime).TotalSeconds;

            _popup.PopupEntity(Loc.GetString("gibtonit-bombhasbeendefused"), uid, PopupType.MediumCaution);
            StopAnimation(uid, comp);
            UpdateAppearance(uid, comp);
        }
        else if (HasComp<SharpComponent>(args.Used))
        {
            if (!comp.Extracted)
                return;

            if (_tag.HasTag(args.Used, PlasticKnife))
            {
                for (var i = 0; i < 3; i++) // спаун 3 кусочков
                {
                    Spawn(comp.ShardPrototype, _transform.GetMapCoordinates(uid));
                }

                QueueDel(uid);
            }
            else
            {
                comp.Active = true;
                comp.ReactionTime = _timing.CurTime;
                comp.ReactionElapsedTime = 0f;
                Explosion(uid, comp);
            }
        }
    }

    /// <summary>
    /// GIVE ME MY FUCKIN ORE.
    /// </summary>
    private void GetOre(EntityUid uid, GibtoniteComponent comp)
    {
        if (comp.Active)
            return;

        var ore = Spawn(comp.OrePrototype, _transform.GetMapCoordinates(uid));
        if (TryComp<GibtoniteComponent>(ore, out var oreComp))
        {
            oreComp.Extracted = true;
            oreComp.ReactionElapsedTime = comp.ReactionElapsedTime;
            oreComp.ReactionMaxTime -= comp.ReactionElapsedTime - 1;

            var gibtoniteSystem = EntityManager.EntitySysManager.GetEntitySystem<GibtoniteSystem>();
            gibtoniteSystem.Explosion(ore, oreComp);
        }

        QueueDel(uid);
    }

    /// <summary>
    /// Остановка анимации гибтонита в камне.
    /// </summary>
    private void StopAnimation(EntityUid uid, GibtoniteComponent comp, AppearanceComponent? appearance = null)
    {
        if (!Resolve(uid, ref appearance, false))
            return;

        _appearance.SetData(uid, TriggerVisuals.VisualState, "Unprimed", appearance);
    }
}