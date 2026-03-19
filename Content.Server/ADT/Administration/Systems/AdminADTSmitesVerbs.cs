using System.Globalization;
using Content.Server.ADT.SpeedBoostWake;
using Content.Shared.Administration;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Verbs;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server.Administration.Systems;

public sealed partial class AdminVerbSystem
{
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;

    // All smite verbs have names so invokeverb works.
    private void AddAdminADTSmitesVerbs(GetVerbsEvent<Verb> args)
    {
        if (!EntityManager.TryGetComponent(args.User, out ActorComponent? actor))
            return;

        var player = actor.PlayerSession;

        // 1984.
        if (HasComp<MapComponent>(args.Target) || HasComp<MapGridComponent>(args.Target))
            return;

        // Изменение ХП сущности — проверка на DEBUG отдельно
        // TODO: Потом если появятся ещё смайт для разных
        // Админ флагов, стоит разделить код а не писать всё в одной функции
        if (_adminManager.HasAdminFlag(player, AdminFlags.Debug) &&
            TryComp<MobThresholdsComponent>(args.Target, out var thresholdsComponent))
        {
            Verb thresholdVerb = new()
            {
                Text = Loc.GetString("admin-smite-threshold-name"),
                Category = VerbCategory.Debug,
                Icon = new SpriteSpecifier.Texture(new("/Textures/ADT/Interface/VerbIcons/icon-tweak-health-smite.png")),
                Act = () =>
                {
                    var thresholds = thresholdsComponent.Thresholds;

                    var fieldNames = new List<string>();
                    var defaultValues = new List<string>();

                    foreach (var kv in thresholds)
                    {
                        fieldNames.Add($"{kv.Value}");          // Имя состояния
                        defaultValues.Add(kv.Key.ToString());   // Значение
                    }

                    _quickDialog.OpenDialogDynamic(
                        player,
                        Loc.GetString("admin-smite-threshold-ui-text"),
                        fieldNames.ToArray(),
                        defaultValues.ToArray(),
                        results =>
                        {
                            var newThresholds = new SortedDictionary<FixedPoint2, MobState>();
                            int i = 0;

                            foreach (var kv in thresholds)
                            {
                                var input = results[i];
                                FixedPoint2 finalValue;

                                if (string.IsNullOrWhiteSpace(input))
                                {
                                    // Пустое поле — оставляем старое значение
                                    finalValue = kv.Key;
                                }
                                else if (float.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out var newValue))
                                {
                                    // Ограничиваем диапазон
                                    if (newValue < 0) newValue = 0;
                                    if (newValue > 10000) newValue = 10000;

                                    finalValue = (FixedPoint2)newValue;
                                }
                                else
                                {
                                    // Некорректный ввод — оставляем старое
                                    finalValue = kv.Key;
                                }
                                // Проверка на дубликаты
                                if (!newThresholds.ContainsKey(finalValue))
                                {
                                    newThresholds[finalValue] = kv.Value;
                                }
                                else
                                {
                                    // Если дубликат — оставляем оригинальный порог
                                    newThresholds[kv.Key] = kv.Value;
                                }

                                i++;
                            }

                            thresholdsComponent.Thresholds = newThresholds;
                            Dirty(args.Target, thresholdsComponent);
                        });
                },
                Impact = LogImpact.Extreme,
                Message = Loc.GetString("admin-smite-threshold-description")
            };
            args.Verbs.Add(thresholdVerb);
        }

        if (!_adminManager.HasAdminFlag(player, AdminFlags.Fun))
            return;

        if (TryComp<PhysicsComponent>(args.Target, out var _))
        {
            var superBoostSpeedName = Loc.GetString("admin-smite-speed-boost-name").ToLowerInvariant();
            Verb superBoostSpeed = new()
            {
                Text = superBoostSpeedName,
                Category = VerbCategory.Smite,
                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/Alerts/walking.rsi/walking.png")),
                Act = () =>
                {
                    var hadSlipComponent = TryComp<SpeedBoostWakeComponent>(args.Target, out var _);

                    if (hadSlipComponent)
                        RemComp<SpeedBoostWakeComponent>(args.Target);
                    else
                        EnsureComp<SpeedBoostWakeComponent>(args.Target);
                },
                Impact = LogImpact.Extreme,
                Message = string.Join(": ", superBoostSpeedName, Loc.GetString("admin-smite-speed-boost-description"))
            };
            args.Verbs.Add(superBoostSpeed);
        }

        // Created by d_bamboni for schrodinger71
        if (TryComp<InputMoverComponent>(args.Target, out var _))
        {
            Verb divineDelay = new()
            {
                Text = Loc.GetString("admin-smite-divine-delay-name"),
                Category = VerbCategory.Smite,
                Icon = new SpriteSpecifier.Texture(new("/Textures/ADT/Interface/Alerts/AdminSmite/divineDelay.png")),
                Act = () =>
                {
                    _quickDialog.OpenDialog(player, "Settings", "Radius", "Damage", "Time Span",
                    (string sRadius, string sDamage, string sTimeSpan) =>
                        {
                            if (!float.TryParse(sRadius, out var radius) || !int.TryParse(sDamage, out var damage) || !int.TryParse(sTimeSpan, out var timeSpan))
                                return;
                            if (radius < 0)
                                radius = 999f;  // Искуственный выход за рамки :)
                            if (damage < 0)
                                damage = 999;  // Искуственный выход за рамки :)
                            if (timeSpan < 0)
                                timeSpan = 999; // Искуственный выход за рамки :)
                            var xform = Transform(args.Target);
                            foreach (var entity in _entityLookup.GetEntitiesInRange(xform.Coordinates, radius))
                            {
                                if (TryComp<InputMoverComponent>(entity, out var _))
                                {
                                    _electrocutionSystem.TryDoElectrocution(entity, null, damage, TimeSpan.FromSeconds(timeSpan), refresh: true, ignoreInsulation: true);
                                    Spawn("Lightning", Transform(entity).Coordinates);
                                }
                            }
                        });
                },
                Impact = LogImpact.Extreme,
                Message = Loc.GetString("admin-smite-divine-delay-description")
            };
            args.Verbs.Add(divineDelay);
        }

    }

}

/*
    ╔════════════════════════════════════╗
    ║   Schrödinger's Cat Code   🐾      ║
    ║   /\_/\\                           ║
    ║  ( o.o )  Meow!                    ║
    ║   > ^ <                            ║
    ╚════════════════════════════════════╝
*/
