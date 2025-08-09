/// Updated Sacrifice System with guaranteed priority handling
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
using System.Linq;

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

    private void OnInteractUsing(EntityUid altar, SacrificeComponent component, InteractUsingEvent args)
    {
        if (args.Handled || _netManager.IsClient)
            return;

        if (!HasComp<ChaplainComponent>(args.User))
        {
            _popup.PopupEntity(Loc.GetString("altar-only-chaplain-altar-use"), args.User, args.User);
            return;
        }

        if (!TryComp<ChaplainComponent>(args.User, out var chaplainComp))
            return;

        var sacrificeOption = FindBestSacrificeOption(args.Used, component);
        if (sacrificeOption == null)
            return;

        if (TryComp<StackComponent>(args.Used, out var stack))
        {
            ProcessStackSacrifice(altar, args, sacrificeOption.Value, stack, chaplainComp);
        }
        else
        {
            ProcessSingleSacrifice(altar, args, sacrificeOption.Value, chaplainComp);
        }

        args.Handled = true;
    }

    private SacrificeOption? FindBestSacrificeOption(EntityUid item, SacrificeComponent component)
    {
        if (TryComp<StackComponent>(item, out var stack))
        {
            return FindBestStackSacrifice(item, stack.Count, component);
        }
        return FindBestSingleSacrifice(item, component);
    }

    private SacrificeOption? FindBestStackSacrifice(EntityUid item, int stackCount, SacrificeComponent component)
    {
        const int threshold = 1500;
        var majorGroup = new List<TransformationData>();
        var minorGroup = new List<TransformationData>();

        foreach (var transform in component.PossibleTransformations)
        {
            if (!MeetsRequirements(item, transform))
                continue;

            if (stackCount < transform.RequiredAmount)
                continue;

            if (transform.RequiredAmount >= threshold)
                majorGroup.Add(transform);
            else
                minorGroup.Add(transform);
        }

        List<TransformationData> applicable = majorGroup.Count > 0 ? majorGroup : minorGroup;
        if (applicable.Count == 0)
            return null;

        var bestTransform = applicable
            .OrderByDescending(t => t.Priority)
            .ThenByDescending(t => t.RequiredAmount)
            .First();

        int times = stackCount / bestTransform.RequiredAmount;
        return new SacrificeOption(bestTransform, times);
    }

    private SacrificeOption? FindBestSingleSacrifice(EntityUid item, SacrificeComponent component)
    {
        return component.PossibleTransformations
            .Where(t => MeetsRequirements(item, t))
            .OrderByDescending(t => t.Priority)
            .Select(t => new SacrificeOption(t, 1))
            .FirstOrDefault();
    }

    private bool MeetsRequirements(EntityUid item, TransformationData transform)
    {
        if (!string.IsNullOrEmpty(transform.RequiredTag) &&
            _tagSystem.HasTag(item, transform.RequiredTag))
        {
            return true;
        }

        if (!string.IsNullOrEmpty(transform.RequiredProto))
        {
            var meta = MetaData(item);
            if (meta.EntityPrototype?.ID == transform.RequiredProto)
            {
                return true;
            }
        }

        return false;
    }

    private void ProcessStackSacrifice(EntityUid altar, InteractUsingEvent args, SacrificeOption option, StackComponent stack, ChaplainComponent chaplainComp)
    {
        var transform = option.Transformation;
        FixedPoint2 totalCost = transform.PowerCost * option.Times;

        if (chaplainComp.Power < totalCost)
        {
            _popup.PopupEntity(Loc.GetString("chaplain-not-enough-power"), args.User, args.User);
            return;
        }

        int itemsToRemove = transform.RequiredAmount * option.Times;
        if (stack.Count < itemsToRemove)
        {
            _popup.PopupEntity(Loc.GetString("chaplain-not-enough-items"), args.User, args.User);
            return;
        }

        // Исправление: частичное уменьшение стака
        _stack.SetCount(args.Used, stack.Count - itemsToRemove, stack);

        // Удаляем только если стак пуст
        if (stack.Count == 0)
        {
            QueueDel(args.Used);
        }

        chaplainComp.Power -= totalCost;
        UpdatePowerAlert(args.User, chaplainComp);

        for (int i = 0; i < option.Times; i++)
        {
            ExecuteTransformation(altar, args, transform);
        }
    }

    private void ProcessSingleSacrifice(EntityUid altar, InteractUsingEvent args, SacrificeOption option, ChaplainComponent chaplainComp)
    {
        var transform = option.Transformation;

        if (chaplainComp.Power < transform.PowerCost)
        {
            _popup.PopupEntity(Loc.GetString("chaplain-not-enough-power"), args.User, args.User);
            return;
        }

        chaplainComp.Power -= transform.PowerCost;
        UpdatePowerAlert(args.User, chaplainComp);

        QueueDel(args.Used);
        ExecuteTransformation(altar, args, transform);
    }

    private void UpdatePowerAlert(EntityUid uid, ChaplainComponent component)
    {
        var level = (short)Math.Clamp(Math.Round(component.Power.Float()), 0, 5);
        var alertType = _protoMan.Index<AlertPrototype>(component.Alert);
        _alertsSystem.ShowAlert(uid, alertType, level);
    }

    private void ExecuteTransformation(EntityUid altar, InteractUsingEvent args, TransformationData transform)
    {
        if (!string.IsNullOrEmpty(transform.EffectProto))
        {
            Spawn(transform.EffectProto, Transform(altar).Coordinates);
        }

        if (transform.SoundPath is not null)
        {
            _audio.PlayPvs(transform.SoundPath, altar, AudioParams.Default.WithVolume(-4f));
        }

        if (!string.IsNullOrEmpty(transform.ResultProto))
        {
            Spawn(transform.ResultProto, Transform(altar).Coordinates);
        }
    }

    private readonly struct SacrificeOption
    {
        public readonly TransformationData Transformation;
        public readonly int Times;

        public SacrificeOption(TransformationData transformation, int times)
        {
            Transformation = transformation;
            Times = times;
        }
    }
}