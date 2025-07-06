using Content.Shared.Bible.Components;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Interaction;
using Content.Shared.Storage;
using Content.Shared.Tag;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Content.Shared.Materials;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Content.Shared.FixedPoint;
using Content.Shared.Alert;
using System;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.ADT.Shared.Chaplain.Sacrifice;

public sealed class SacrificeSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly INetManager _netManager = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly SharedStackSystem _stack = default!;
    [Dependency] private readonly AlertsSystem _alertsSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SacrificeComponent, InteractUsingEvent>(OnInteractUsing);
    }

    private TransformationData? GetApplicableTransformation(EntityUid used, SacrificeComponent component)
    {
        // Check for stacks
        if (TryComp<StackComponent>(used, out var stack))
        {
            foreach (var transform in component.PossibleTransformations)
            {
                if (CheckStackTransformation(used, transform, stack))
                {
                    return transform;
                }
            }
        }

        // Check for tags
        foreach (var transform in component.PossibleTransformations)
        {
            if (!string.IsNullOrEmpty(transform.RequiredTag) &&
                _tagSystem.HasTag(used, transform.RequiredTag))
            {
                return transform;
            }
        }

        // Check for prototypes
        var meta = MetaData(used);
        var protoId = meta.EntityPrototype?.ID;
        foreach (var transform in component.PossibleTransformations)
        {
            if (!string.IsNullOrEmpty(transform.RequiredProto) &&
                protoId == transform.RequiredProto)
            {
                return transform;
            }
        }

        return null;
    }

    private void OnInteractUsing(EntityUid altar, SacrificeComponent component, InteractUsingEvent args)
    {
        if (args.Handled || _netManager.IsClient)
            return;

        // Проверка, что игрок - священник
        if (!HasComp<ChaplainComponent>(args.User))
        {
            _popup.PopupEntity(
                Loc.GetString("altar-only-chaplain-altar-use"),
                args.User,
                args.User
            );
            return;
        }

        var transformation = GetApplicableTransformation(args.Used, component);
        if (transformation == null)
        {
            return;
        }

        // Проверка очков силы
        if (!TryComp<ChaplainComponent>(args.User, out var chaplainComp))
        {
            return;
        }

        if (chaplainComp.Power < transformation.PowerCost)
        {
            _popup.PopupEntity(
                Loc.GetString("chaplain-not-enough-power"),
                args.User,
                args.User
            );
            return;
        }

        // Выполнение жертвоприношения
        if (TryComp<StackComponent>(args.Used, out var stack))
        {
            ProcessStackSacrifice(altar, args, transformation, stack, chaplainComp);
        }
        else
        {
            ProcessSacrifice(altar, args, transformation, chaplainComp);
        }

        args.Handled = true;
    }

    private void UpdatePowerAlert(EntityUid uid, ChaplainComponent component)
    {
        // Обновляем алерт напрямую через AlertsSystem
        var level = (short) Math.Clamp(Math.Round(component.Power.Float()), 0, 5);
        var alertType = _protoMan.Index<AlertPrototype>(component.Alert);
        _alertsSystem.ShowAlert(uid, alertType, level);
    }

    private bool CheckStackTransformation(EntityUid used, TransformationData transform, StackComponent stack)
    {
        // Для стаков проверяем по тегу
        if (!string.IsNullOrEmpty(transform.RequiredTag) &&
            _tagSystem.HasTag(used, transform.RequiredTag))
        {
            return true;
        }

        // Для стаков проверяем по прототипу
        if (!string.IsNullOrEmpty(transform.RequiredProto))
        {
            var meta = MetaData(used);
            var protoId = meta.EntityPrototype?.ID;

            if (protoId == transform.RequiredProto &&
                stack.Count >= transform.RequiredAmount)
            {
                return true;
            }
        }

        return false;
    }

    private void ProcessStackSacrifice(EntityUid altar, InteractUsingEvent args, TransformationData transform, StackComponent stack, ChaplainComponent chaplainComp)
    {
        // Рассчитываем, сколько полных трансформаций мы можем выполнить
        int possibleTransformations = stack.Count / transform.RequiredAmount;
        if (possibleTransformations <= 0) return;

        // Рассчитываем общую стоимость силы
        FixedPoint2 totalPowerCost = transform.PowerCost * possibleTransformations;

        // Проверяем, достаточно ли силы для всех трансформаций
        if (chaplainComp.Power < totalPowerCost)
        {
            _popup.PopupEntity(
                Loc.GetString("chaplain-not-enough-power"),
                args.User,
                args.User
            );
            return;
        }

        // Уменьшаем силу
        chaplainComp.Power -= totalPowerCost;
        UpdatePowerAlert(args.User, chaplainComp);

        // Рассчитываем, сколько предметов нужно удалить
        int itemsToRemove = possibleTransformations * transform.RequiredAmount;

        // Уменьшаем стек
        _stack.SetCount(args.Used, stack.Count - itemsToRemove, stack);

        // Если количество достигло нуля - удаляем
        if (stack.Count - itemsToRemove <= 0)
        {
            QueueDel(args.Used);
        }

        // Выполняем трансформации
        for (int i = 0; i < possibleTransformations; i++)
        {
            ExecuteTransformation(altar, args, transform);
        }
    }

    private void ProcessSacrifice(EntityUid altar, InteractUsingEvent args, TransformationData transform, ChaplainComponent chaplainComp)
    {
        // Уменьшение очков силы
        chaplainComp.Power -= transform.PowerCost;
        UpdatePowerAlert(args.User, chaplainComp);

        QueueDel(args.Used);
        ExecuteTransformation(altar, args, transform);
    }

    private void ExecuteTransformation(EntityUid altar, InteractUsingEvent args, TransformationData transform)
    {
        // Визуальный эффект
        if (!string.IsNullOrEmpty(transform.EffectProto))
        {
            Spawn(transform.EffectProto, Transform(altar).Coordinates);
        }

        // Звуковой эффект
        if (transform.SoundPath is not null)
        {
            _audio.PlayPvs(transform.SoundPath, altar, AudioParams.Default.WithVolume(-4f));
        }

        // Создание результата
        if (!string.IsNullOrEmpty(transform.ResultProto))
        {
            Spawn(transform.ResultProto, args.ClickLocation.SnapToGrid(EntityManager));
        }
    }
}