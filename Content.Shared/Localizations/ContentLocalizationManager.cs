using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Robust.Shared.Utility;
using Robust.Shared.ContentPack;
using Linguini.Syntax.Parser;
using System.IO;

namespace Content.Shared.Localizations
{
    public sealed class ContentLocalizationManager
    {
        [Dependency] private readonly ILocalizationManager _loc = default!;

        private static readonly ISawmill Sawmill = Logger.GetSawmill("content.localization");

        // If you want to change your codebase's language, do it here.
        private const string Culture = "ru-RU"; // Corvax-Localization
        private const string FallbackCulture = "en-US"; // Corvax-Localization

        /// <summary>
        /// Custom format strings used for parsing and displaying minutes:seconds timespans.
        /// </summary>
        public static readonly string[] TimeSpanMinutesFormats = new[]
        {
            @"m\:ss",
            @"mm\:ss",
            @"%m",
            @"mm"
        };

        public void Initialize()
        {
            var culture = new CultureInfo(Culture);
            var fallbackCulture = new CultureInfo(FallbackCulture); // Corvax-Localization

            SafeLoadCultureWithDetailedErrors(culture);
            SafeLoadCultureWithDetailedErrors(fallbackCulture);

            _loc.SetFallbackCluture(fallbackCulture); // Corvax-Localization
            _loc.AddFunction(culture, "MANY", FormatMany); // Corvax-Localization: To prevent problems in auto-generated locale files
            _loc.AddFunction(culture, "PRESSURE", FormatPressure);
            _loc.AddFunction(culture, "POWERWATTS", FormatPowerWatts);
            _loc.AddFunction(culture, "POWERJOULES", FormatPowerJoules);
            _loc.AddFunction(culture, "ENERGYWATTHOURS", FormatEnergyWattHours);
            _loc.AddFunction(culture, "UNITS", FormatUnits);
            _loc.AddFunction(culture, "TOSTRING", args => FormatToString(culture, args));
            _loc.AddFunction(culture, "LOC", FormatLoc);
            _loc.AddFunction(culture, "NATURALFIXED", FormatNaturalFixed);
            _loc.AddFunction(culture, "NATURALPERCENT", FormatNaturalPercent);
            _loc.AddFunction(culture, "PLAYTIME", FormatPlaytime);
            _loc.AddFunction(culture, "PLAYTIMEMINUTES", FormatPlaytimeMinutes); // ADT Change

            /*
             * The following language functions are specific to the english localization. When working on your own
             * localization you should NOT modify these, instead add new functions specific to your language/culture.
             * This ensures the english translations continue to work as expected when fallbacks are needed.
             */
            var cultureEn = new CultureInfo("en-US");

            _loc.AddFunction(cultureEn, "MAKEPLURAL", FormatMakePlural);
            _loc.AddFunction(cultureEn, "MANY", FormatMany);
        }

        private void SafeLoadCultureWithDetailedErrors(CultureInfo culture)
        {
            var resourceManager = IoCManager.Resolve<IResourceManager>();

            var dir = new ResPath($"/Locale/{culture.Name}");
            var files = resourceManager.ContentFindFiles(dir)
                .Where(c => c.Filename.EndsWith(".ftl", System.StringComparison.InvariantCultureIgnoreCase));

            foreach (var path in files)
            {
                Sawmill.Info($"Проверка файла локализации: {path}");
                try
                {
                    using var stream = resourceManager.ContentFileRead(path);
                    using var reader = new StreamReader(stream, Encoding.UTF8);
                    var contents = reader.ReadToEnd();

                    var parser = new LinguiniParser(contents);
                    var resource = parser.Parse();

                    if (resource.Errors.Count > 0)
                    {
                        Sawmill.Warning($"Ошибки парсинга в {path}:");
                        foreach (var err in resource.Errors)
                            Sawmill.Warning($"{err.Kind} at {err.Slice}");
                    }
                }
                catch (System.Exception e)
                {
                    Sawmill.Error($"Ошибка с файлом {path}: {e}");
                }
            }

            _loc.LoadCulture(culture);
        }

        private ILocValue FormatMany(LocArgs args)
        {
            var count = ((LocValueNumber) args.Args[1]).Value;

            if (System.Math.Abs(count - 1) < 0.0001f)
            {
                return (LocValueString) args.Args[0];
            }
            else
            {
                return (LocValueString) FormatMakePlural(args);
            }
        }

        private ILocValue FormatNaturalPercent(LocArgs args)
        {
            var number = ((LocValueNumber) args.Args[0]).Value * 100;
            var maxDecimals = (int)System.Math.Floor(((LocValueNumber) args.Args[1]).Value);
            var formatter = (System.Globalization.NumberFormatInfo)System.Globalization.NumberFormatInfo.GetInstance(System.Globalization.CultureInfo.GetCultureInfo(Culture)).Clone();
            formatter.NumberDecimalDigits = maxDecimals;
            return new LocValueString(string.Format(formatter, "{0:N}", number).TrimEnd('0').TrimEnd(char.Parse(formatter.NumberDecimalSeparator)) + "%");
        }

        private ILocValue FormatNaturalFixed(LocArgs args)
        {
            var number = ((LocValueNumber) args.Args[0]).Value;
            var maxDecimals = (int)System.Math.Floor(((LocValueNumber) args.Args[1]).Value);
            var formatter = (System.Globalization.NumberFormatInfo)System.Globalization.NumberFormatInfo.GetInstance(System.Globalization.CultureInfo.GetCultureInfo(Culture)).Clone();
            formatter.NumberDecimalDigits = maxDecimals;
            return new LocValueString(string.Format(formatter, "{0:N}", number).TrimEnd('0').TrimEnd(char.Parse(formatter.NumberDecimalSeparator)));
        }

        private static readonly Regex PluralEsRule = new("^.*(s|sh|ch|x|z)$");

        private ILocValue FormatMakePlural(LocArgs args)
        {
            var text = ((LocValueString) args.Args[0]).Value;
            var split = text.Split(" ", 1);
            var firstWord = split[0];
            if (PluralEsRule.IsMatch(firstWord))
            {
                if (split.Length == 1)
                    return new LocValueString($"{firstWord}es");
                else
                    return new LocValueString($"{firstWord}es {split[1]}");
            }
            else
            {
                if (split.Length == 1)
                    return new LocValueString($"{firstWord}s");
                else
                    return new LocValueString($"{firstWord}s {split[1]}");
            }
        }

        public static string FormatList(System.Collections.Generic.List<string> list)
        {
            return list.Count switch
            {
                <= 0 => string.Empty,
                1 => list[0],
                2 => $"{list[0]} and {list[1]}",
                _ => $"{string.Join(", ", list.GetRange(0, list.Count - 1))}, and {list[^1]}"
            };
        }

        public static string FormatListToOr(System.Collections.Generic.List<string> list)
        {
            return list.Count switch
            {
                <= 0 => string.Empty,
                1 => list[0],
                2 => $"{list[0]} or {list[1]}",
                _ => $"{string.Join(" or ", list)}"
            };
        }

        public static string FormatDirection(Direction dir)
        {
            return Loc.GetString($"zzzz-fmt-direction-{dir.ToString()}");
        }

        public static string FormatPlaytime(System.TimeSpan time)
        {
            time = System.TimeSpan.FromMinutes(System.Math.Ceiling(time.TotalMinutes));
            var hours = (int)time.TotalHours;
            var minutes = time.Minutes;
            return Loc.GetString($"zzzz-fmt-playtime", ("hours", hours), ("minutes", minutes));
        }

        public static string FormatPlaytimeMinutes(System.TimeSpan time)
        {
            time = System.TimeSpan.FromMinutes(System.Math.Ceiling(time.TotalMinutes));
            var minutes = (int)System.Math.Ceiling(time.TotalMinutes);
            return Loc.GetString($"zzzz-fmt-playtime-minutes", ("minutes", minutes));
        }

        private static ILocValue FormatLoc(LocArgs args)
        {
            var id = ((LocValueString) args.Args[0]).Value;

            return new LocValueString(Loc.GetString(id, args.Options.Select(x => (x.Key, x.Value.Value!)).ToArray()));
        }

        private static ILocValue FormatToString(CultureInfo culture, LocArgs args)
        {
            var arg = args.Args[0];
            var fmt = ((LocValueString) args.Args[1]).Value;

            var obj = arg.Value;
            if (obj is System.IFormattable formattable)
                return new LocValueString(formattable.ToString(fmt, culture));

            return new LocValueString(obj?.ToString() ?? "");
        }

        private static ILocValue FormatUnitsGeneric(
            LocArgs args,
            string mode,
            System.Func<double, double>? transformValue = null)
        {
            const int maxPlaces = 5; // Matches amount in _lib.ftl
            var pressure = ((LocValueNumber) args.Args[0]).Value;

            if (transformValue != null)
                pressure = transformValue(pressure);

            var places = 0;
            while (pressure > 1000 && places < maxPlaces)
            {
                pressure /= 1000;
                places += 1;
            }

            return new LocValueString(Loc.GetString(mode, ("divided", pressure), ("places", places)));
        }

        private static ILocValue FormatPressure(LocArgs args)
        {
            return FormatUnitsGeneric(args, "zzzz-fmt-pressure");
        }

        private static ILocValue FormatPowerWatts(LocArgs args)
        {
            return FormatUnitsGeneric(args, "zzzz-fmt-power-watts");
        }

        private static ILocValue FormatPowerJoules(LocArgs args)
        {
            return FormatUnitsGeneric(args, "zzzz-fmt-power-joules");
        }

        private static ILocValue FormatEnergyWattHours(LocArgs args)
        {
            const double joulesToWattHours = 1.0 / 3600;

            return FormatUnitsGeneric(args, "zzzz-fmt-energy-watt-hours", joules => joules * joulesToWattHours);
        }

        private static ILocValue FormatUnits(LocArgs args)
        {
            if (!Units.Types.TryGetValue(((LocValueString) args.Args[0]).Value, out var ut))
                throw new System.ArgumentException($"Unknown unit type {((LocValueString) args.Args[0]).Value}");

            var fmtstr = ((LocValueString) args.Args[1]).Value;

            double max = double.NegativeInfinity;
            var iargs = new double[args.Args.Count - 1];
            for (var i = 2; i < args.Args.Count; i++)
            {
                var n = ((LocValueNumber) args.Args[i]).Value;
                if (n > max)
                    max = n;

                iargs[i - 2] = n;
            }

            if (!ut.TryGetUnit(max, out var mu))
                throw new System.ArgumentException("Unit out of range for type");

            var fargs = new object[iargs.Length];

            for (var i = 0; i < iargs.Length; i++)
                fargs[i] = iargs[i] * mu.Factor;

            fargs[^1] = Loc.GetString($"units-{mu.Unit.ToLower()}");

            var res = string.Format(
                fmtstr.Replace("{UNIT", "{" + $"{fargs.Length - 1}"),
                fargs
            );

            return new LocValueString(res);
        }

        private static ILocValue FormatPlaytime(LocArgs args)
        {
            var time = System.TimeSpan.Zero;
            if (args.Args is { Count: > 0 } && args.Args[0].Value is System.TimeSpan timeArg)
            {
                time = timeArg;
            }
            return new LocValueString(FormatPlaytime(time));
        }

        private static ILocValue FormatPlaytimeMinutes(LocArgs args) // ADT Changes start
        {
            var time = System.TimeSpan.Zero;
            if (args.Args is { Count: > 0 } && args.Args[0].Value is System.TimeSpan timeArg)
            {
                time = timeArg;
            }
            return new LocValueString(FormatPlaytimeMinutes(time));
        } // ADT Changes end
    }
}
