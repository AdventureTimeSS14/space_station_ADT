using Content.Server.Body.Components;
using Content.Shared.ADT.ReliveResuscitation;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.DoAfter;
using Content.Shared.IdentityManagement;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using Robust.Shared.Timing;


namespace Content.Server.ADT.ReliveResuscitation;

//SharedReliveResuscitationSystem
public sealed partial class ReliveResuscitationSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ReliveResuscitationComponent, GetVerbsEvent<AlternativeVerb>>(OnAltVerbs);
        SubscribeLocalEvent<ReliveResuscitationComponent, ReliveDoAfterEvent>(DoRelive);
    }

    /// <summary>
    /// Обрабатывает событие получения альтернативных действий для сущности (на ПКМ).
    /// Если сущность находится в критическом состоянии, добавляет возможность провести сердечно-лёгочную реанимацию.
    /// </summary>
    /// <param name="uid">Идентификатор сущности, на которой выполняется действие.</param>
    /// <param name="component">Компонент реанимации, связанный с сущностью.</param>
    /// <param name="args">Событие, содержащее информацию о доступных альтернативных действиях.</param>
    private void OnAltVerbs(EntityUid uid, ReliveResuscitationComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (TryComp<MobStateComponent>(uid, out var mobState) && mobState.CurrentState == MobState.Critical)
        {
            // TODO: Можно конечно всё усложнить с дыханием, и чекать совпадает ли оно...
            if (!HasComp<ReliveResuscitationComponent>(args.User))
                return;     // Думаю что юзер тоже такой компонент должен иметь...
                            // проблем в будущем не должно создать

            AlternativeVerb verbPersonalize = new()
            {
                Act = () => Relive(uid, args.User, component, mobState),
                Text = Loc.GetString("relive-resuscitation-verb"),
                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/rejuvenate.svg.192dpi.png")),
            };

            args.Verbs.Add(verbPersonalize);
        }
    }

    /// <summary>
    /// Начинает процесс реанимации сущности. Отправляет сообщение пользователю и запускает таймер действия.
    /// </summary>
    /// <param name="uid">Идентификатор сущности, которую пытаются реанимировать.</param>
    /// <param name="user">Идентификатор пользователя, который проводит реанимацию.</param>
    /// <param name="component">Компонент реанимации, связанный с сущностью.</param>
    /// <param name="mobState">Компонент состояния сущности, указывающий текущее состояние.</param>
    private void Relive(EntityUid uid, EntityUid user, ReliveResuscitationComponent component, MobStateComponent mobState)
    {
        // if (!_timing.IsFirstTimePredicted)
        //     return;

        if (mobState.CurrentState != MobState.Critical)
            return;

        var stringLoc = Loc.GetString("relive-start-message", ("user", Identity.Entity(user, EntityManager)),
        ("name", Identity.Entity(uid, EntityManager)));

        _popup.PopupEntity(stringLoc, uid, user); // Сообщение о том что начали делать СЛР. (Кто) и (кому)

        var doAfterEventArgs =
            new DoAfterArgs(EntityManager, user, component.Delay, new ReliveDoAfterEvent() { Repeat = true }, uid, target: uid, used: user)
            {
                NeedHand = true,
                BreakOnMove = true,
                BreakOnWeightlessMove = false,
                BreakOnDamage = true,
            };


        _doAfter.TryStartDoAfter(doAfterEventArgs);
    }

    /// <summary>
    /// Завершает процесс реанимации, нанося урон от удушения и добавляя грубый урон.
    /// Проверяет состояние сущности перед применением урона.
    /// </summary>
    /// <param name="uid">Идентификатор сущности, которую пытаются реанимировать.</param>
    /// <param name="component">Компонент реанимации, связанный с сущностью.</param>
    /// <param name="args">Событие, содержащее информацию о выполнении действия.</param>
    private void DoRelive(EntityUid uid, ReliveResuscitationComponent component, ref ReliveDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        var randomDamageAshyxiation = _random.Next(component.MaxAsphyxiationHeal, component.MinAsphyxiationHeal);
        var randomDamageBlunt = _random.Next(component.MinBluntDamage, component.MaxBluntDamage);

        DamageSpecifier damageAsphyxiation = new(_prototypeManager.Index<DamageTypePrototype>(component.DamageAsphyxiation),
        randomDamageAshyxiation);
        DamageSpecifier damageBlunt = new(_prototypeManager.Index<DamageTypePrototype>(component.DamageBlunt),
        randomDamageBlunt);

        _damageable.TryChangeDamage(uid, damageAsphyxiation, true);
        _damageable.TryChangeDamage(uid, damageBlunt, true);

        args.Handled = true;
        args.Repeat = true;

        if (!TryComp<MobStateComponent>(uid, out var mobState) || mobState.CurrentState != MobState.Critical)
        {
            var locReliveAbort = Loc.GetString("relive-abort-message",
            ("name", Identity.Entity(uid, EntityManager)));

            _popup.PopupEntity(locReliveAbort, uid, args.User); // by ideas Mirokko :з

            args.Repeat = false;
            return;
        }
    }
}
