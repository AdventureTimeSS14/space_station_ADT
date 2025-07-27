/// What can i say exept "Your welcome"?. Update made by QWERTY
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

        // checking for Chaplain component in players character
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

        // Checking for faith power points
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

        // doin' sacrafice
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
        var level = (short) Math.Clamp(Math.Round(component.Power.Float()), 0, 5);
        var alertType = _protoMan.Index<AlertPrototype>(component.Alert);
        _alertsSystem.ShowAlert(uid, alertType, level);
    }

    private bool CheckStackTransformation(EntityUid used, TransformationData transform, StackComponent stack)
    {
        if (!string.IsNullOrEmpty(transform.RequiredTag) &&
            _tagSystem.HasTag(used, transform.RequiredTag))
        {
            return true;
        }

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
        int possibleTransformations = stack.Count / transform.RequiredAmount;
        if (possibleTransformations <= 0) return;

        FixedPoint2 totalPowerCost = transform.PowerCost * possibleTransformations;
        if (chaplainComp.Power < totalPowerCost)
        {
            _popup.PopupEntity(
                Loc.GetString("chaplain-not-enough-power"),
                args.User,
                args.User
            );
            return;
        }

        chaplainComp.Power -= totalPowerCost;
        UpdatePowerAlert(args.User, chaplainComp);

        int itemsToRemove = possibleTransformations * transform.RequiredAmount;

        _stack.SetCount(args.Used, stack.Count - itemsToRemove, stack);

        if (stack.Count - itemsToRemove <= 0)
        {
            QueueDel(args.Used);
        }

        for (int i = 0; i < possibleTransformations; i++)
        {
            ExecuteTransformation(altar, args, transform);
        }
    }

    private void ProcessSacrifice(EntityUid altar, InteractUsingEvent args, TransformationData transform, ChaplainComponent chaplainComp)
    {
        chaplainComp.Power -= transform.PowerCost;
        UpdatePowerAlert(args.User, chaplainComp);

        QueueDel(args.Used);
        ExecuteTransformation(altar, args, transform);
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
            Spawn(transform.ResultProto, args.ClickLocation.SnapToGrid(EntityManager));
        }
    }
}