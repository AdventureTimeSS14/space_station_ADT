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

        if (!_adminManager.HasAdminFlag(player, AdminFlags.Fun))
            return;

        // 1984.
        if (HasComp<MapComponent>(args.Target) || HasComp<MapGridComponent>(args.Target))
            return;

        if (TryComp<PhysicsComponent>(args.Target, out var _))
        {
            var superBoostSpeedName = Loc.GetString("admin-smite-speed-boost-name").ToLowerInvariant();
            Verb superBoostSpeed = new()
            {
                Text = superBoostSpeedName,
                Category = VerbCategory.Smite,
                Icon = new SpriteSpecifier.Texture(new("ADT/Interface/Alerts/charge.rsi/charge4.png")),
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

        // Изменение ХП сущности
        if (TryComp<MobThresholdsComponent>(args.Target, out var thresholdsComponent))
        {
            Verb thresholdVerb = new()
            {
                Text = Loc.GetString("admin-smite-threshold-name"),
                Category = VerbCategory.Debug,
                Icon = new SpriteSpecifier.Texture(new("/Textures/ADT/Interface/Alerts/AdminSmite/divineDelay.png")),
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
                        "Изменение ХП",
                        fieldNames.ToArray(),
                        defaultValues.ToArray(),
                        results =>
                        {
                            var newThresholds = new SortedDictionary<FixedPoint2, MobState>();
                            int i = 0;
                            foreach (var kv in thresholds)
                            {
                                var input = results[i];
                                if (string.IsNullOrWhiteSpace(input))
                                {
                                    // Если поле пустое — оставляем старое значение
                                    newThresholds[kv.Key] = kv.Value;
                                }
                                else if (float.TryParse(input, out var newValue))
                                {
                                    newThresholds[(FixedPoint2)newValue] = kv.Value;
                                }
                                else
                                {
                                    // Если введено что-то некорректное, тоже оставляем старое
                                    newThresholds[kv.Key] = kv.Value;
                                }
                                i++;
                            }
                            thresholdsComponent.Thresholds = newThresholds;
                        });
                },
                Impact = LogImpact.Extreme,
                Message = Loc.GetString("admin-smite-threshold-description")
            };
            args.Verbs.Add(thresholdVerb);
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
