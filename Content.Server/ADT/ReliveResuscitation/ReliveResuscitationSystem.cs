using Content.Shared.Verbs;
using Robust.Shared.Utility;
using Content.Shared.Popups;
using Content.Shared.ADT.ReliveResuscitation;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Content.Shared.IdentityManagement;
using Content.Shared.DoAfter;
using Content.Server.Body.Components;


namespace Content.Server.ADT.ReliveResuscitation;

public sealed partial class ReliveResuscitationSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
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
        if (!TryComp<MobStateComponent>(uid, out var mobState) || mobState.CurrentState != MobState.Critical)
            return;

        // TODO: Можно конечно всё усложнить с дыханием, и чекать совпадает ли оно...
        if (!HasComp<LungComponent>(uid) || !HasComp<LungComponent>(args.User)
        || !HasComp<ReliveResuscitationComponent>(args.User)) return;   // Думаю что юзер тоже такой компонент должен иметь...
                                                                        // проблем в будущем не должно создать

        AlternativeVerb verbPersonalize = new()
        {
            Act = () => Relive(uid, args.User, component, mobState),
            // TODO: перенести в ftl
            Text = Loc.GetString("Сердечно-лёгочная реанимация"),
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/rejuvenate.svg.192dpi.png")),
        };

        args.Verbs.Add(verbPersonalize);
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
        if (mobState.CurrentState != MobState.Critical)
            return;

        var stringLoc = Loc.GetString("relive-start-message", ("user", Identity.Entity(user, EntityManager)), ("name", Identity.Entity(uid, EntityManager)));
        _popup.PopupEntity(stringLoc, uid, user);
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

        if (!TryComp<MobStateComponent>(uid, out var mobState) || mobState.CurrentState != MobState.Critical)
        {
            // TODO: Места для супер попапа чтобы прервать. by Mirokko <3. Мэээъъъъ
            // . . . . .

            args.Repeat = false;
            return;
        }

        // TODO: Перенести всё в компонент
        FixedPoint2 asphyxiationHeal = -20;
        FixedPoint2 bluntDamage = 3;
        DamageSpecifier damageAsphyxiation = new(_prototypeManager.Index<DamageTypePrototype>("Asphyxiation"), asphyxiationHeal);
        DamageSpecifier damageBlunt = new(_prototypeManager.Index<DamageTypePrototype>("Blunt"), bluntDamage);
        _damageable.TryChangeDamage(uid, damageAsphyxiation, true);
        _damageable.TryChangeDamage(uid, damageBlunt, true);

        args.Handled = true;
    }
}
