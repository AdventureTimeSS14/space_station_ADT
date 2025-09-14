using System.Text.RegularExpressions;
using Content.Server.Speech.Components;
using System.Text;
using System.Globalization;
using System.Linq;

namespace Content.Server.Speech.EntitySystems
{
    public sealed class DeutschAccentSystem : EntitySystem
    {
        private readonly Dictionary<string, string> _replacements = new();
        private Regex? _replaceRegex;

        // Инструкции для TTS-а, чтобы слова произносились правильно
        private readonly Dictionary<string, string> _pronunciations = new()
        {
            {"wozu", "воцу"},
            {"warum", "варум"},
            {"wie", "ви"},
            {"so", "зо"},
            {"mir", "мир"},
            {"was", "вас"},
            {"ja", "я"},
            {"nein", "найт"},
            {"sehr", "зэр"},
            {"du", "ду"},
            {"stehen", "штээн"},
            {"wir", "вир"},
            {"mein", "майт"},
            {"meine", "майнэ"},
            {"bitte", "биттэ"},
            {"bitte sehr", "биттэ зэр"},
            {"bitte, bitte sehr", "биттэ, биттэ зэр"},
            {"entschuldigung", "энтшульдигунг"},
            {"freund", "фройнд"},
            {"zum freund", "цум фройнд"},
            {"freunde", "фройндэ"},
            {"freundchen", "фройндхен"},
            {"freundin", "фройндин"},
            {"zur freundin", "цур фройндин"},
            {"freundinnen", "фройндиннен"},
            {"freundinchen", "фройндинхен"},
            {"kamerad", "камерад"},
            {"zum kameraden", "цум камераден"},
            {"kameraden", "камераден"},
            {"bruder", "брудер"},
            {"zum bruder", "цум брудер"},
            {"brüder", "брюдер"},
            {"schwester", "швестер"},
            {"zur schwester", "цур швестер"},
            {"schwestern", "швестерн"},
            {"vater", "фатер"},
            {"zum vater", "цум фатер"},
            {"väter", "фэтер"},
            {"gut", "гут"},
            {"ausgezeichnet", "ауфгецайхнет"},
            {"prächtig", "прэхтих"},
            {"prächtiger", "прэхтигер"},
            {"prächtige", "прэхтигэ"},
            {"prächtigen", "прэхтиген"},
            {"prächtiges", "прэхтигес"},
            {"herrlich", "хэррлих"},
            {"herrlicher", "хэррлихер"},
            {"herrliche", "хэррлихэ"},
            {"herrlichen", "хэррлихен"},
            {"herrlicher", "хэррлихер"},
            {"herrliche", "хэррлихэ"},
            {"herrliche", "хэррлихэ"},
            {"wunderbar", "вундербар"},
            {"wunderbarer", "вундербарер"},
            {"wunderbare", "вундербарэ"},
            {"wunderbaren", "вундербарен"},
            {"wunderbarer", "вундербарер"},
            {"schön", "шён"},
            {"schöner", "шёнер"},
            {"schöne", "шёнэ"},
            {"schönen", "шёнен"},
            {"schöner", "шёнер"},
            {"schöne", "шёнэ"},
            {"schöne", "шёнэ"},
            {"assistent", "ассистент"},
            {"schwein", "швайн"},
            {"danke", "данкэ"},
            {"danke schön", "данкэ шён"},
            {"danke vielmals", "данкэ фильмальс"},
            {"frau", "фрау"},
            {"hey", "хай"},
            {"mensch", "менш"},
            {"hallo", "халло"},
            {"guten tag", "гутен таг"},
            {"guten morgen", "гутен морген"},
            {"guten abend", "гутен абенд"},
            {"gute nacht", "гутэ нахт"},
            {"tschüss", "чюсс"},
            {"auf wiedersehen", "ауф видерзэен"},
            {"clown", "клоун"},
            {"vamos", "фамос"},
            {"los", "лос"},
            {"mannhund", "манхунд"},
            {"vertilgen", "фертильген"},
            {"verbrennen", "фербреннен"},
            {"töten", "тётен"},
            {"bier", "бир"},
            {"wasser", "вассер"},
            {"agent", "агент"},
            {"zum agenten", "цум агентен"},
            {"agenten", "агентен"},
            {"nukleare agenten", "нуклеарэ агентен"},
            {"einsatzkräfte", "айнзацкрэфтэ"},
            {"terrorist", "террорист"},
            {"terroristen", "террористен"},
            {"korporation", "корпорацион"},
            {"führer", "фюрер"},
            {"führer'a", "фюрер'а"},
            {"chief", "чиф"},
            {"chefarzt", "шефарцт"},
            {"doktorvater", "докторфатер"},
            {"leiter des personals", "лайтер дес персональс"},
            {"leiter des sicherheitsdienstes", "лайтер дес зихерхайтсдинстес"},
            {"quartiermeister", "квартирмайстер"},
            {"polizei", "полицай"},
        };

        // Немецкий акцент
        private Regex? _pronounceRegex;

        public override void Initialize()
        {
            base.Initialize();

            _replacements.Add("зачем", "wozu");
            _replacements.Add("почему", "warum");
            _replacements.Add("как", "wie");
            _replacements.Add("так", "so");
            _replacements.Add("мне", "mir");
            _replacements.Add("что", "was");
            _replacements.Add("да", "ja");
            _replacements.Add("нет", "nein");
            _replacements.Add("очень", "sehr");
            _replacements.Add("ты", "du");
            _replacements.Add("стоять", "stehen");
            _replacements.Add("мы", "wir");

            _replacements.Add("мой", "mein");
            _replacements.Add("моё", "mein");
            _replacements.Add("мои", "meine");
            _replacements.Add("моей", "meiner");
            _replacements.Add("моих", "meiner");
            _replacements.Add("моего", "meines");

            _replacements.Add("пожалуйста", "bitte");
            _replacements.Add("ну пожалуйста", "bitte, bitte sehr");

            _replacements.Add("простите", "entschuldigung");
            _replacements.Add("извините", "entschuldigung");
            _replacements.Add("прошу прощение", "entschuldigung");
            _replacements.Add("прошу прощения", "entschuldigung");

            _replacements.Add("друг", "freund");
            _replacements.Add("другу", "zum freund");
            _replacements.Add("друга", "freund");
            _replacements.Add("друзья", "freunde");
            _replacements.Add("друзей", "freunde");
            _replacements.Add("дружок", "kleiner freund");

            _replacements.Add("подруга", "freundin");
            _replacements.Add("подруге", "zur freundin");
            _replacements.Add("подругу", "freundin");
            _replacements.Add("подруги", "freundinnen");
            _replacements.Add("подруг", "freundinnen");
            _replacements.Add("подружка", "kleine freundin");

            _replacements.Add("товарищ", "kamerad");
            _replacements.Add("товарищу", "zum kameraden");
            _replacements.Add("товарища", "kamerad");
            _replacements.Add("товарищи", "kameraden");
            _replacements.Add("товарищей", "kameraden");

            _replacements.Add("брат", "bruder");
            _replacements.Add("брату", "zum bruder");
            _replacements.Add("брата", "bruder");
            _replacements.Add("братья", "brüder");
            _replacements.Add("братьев", "brüder");

            _replacements.Add("сестра", "schwester");
            _replacements.Add("сестре", "zur schwester");
            _replacements.Add("сестры", "schwester");
            _replacements.Add("сёстры", "schwestern");
            _replacements.Add("сестёр", "schwestern");

            _replacements.Add("отец", "vater");
            _replacements.Add("отцу", "zum vater");
            _replacements.Add("отца", "vater");
            _replacements.Add("отцы", "väter");
            _replacements.Add("отцов", "väter");

            _replacements.Add("хорошо", "gut");
            _replacements.Add("хорош", "gut");
            _replacements.Add("хороша", "gut");
            _replacements.Add("хороший", "gut");
            _replacements.Add("хорошая", "gut");
            _replacements.Add("хороши", "gut");
            _replacements.Add("хороших", "gut");
            _replacements.Add("хорошего", "gut");

            _replacements.Add("отлично", "ausgezeichnet");

            _replacements.Add("великолепно", "prächtig");
            _replacements.Add("великолепен", "prächtiger");
            _replacements.Add("великолепные", "prächtige");
            _replacements.Add("великолепных", "prächtigen");
            _replacements.Add("великолепный", "prächtiger");
            _replacements.Add("великолепна", "prächtige");
            _replacements.Add("великолепная", "prächtige");

            _replacements.Add("замечательно", "herrlich");
            _replacements.Add("замечателен", "herrlicher");
            _replacements.Add("замечательные", "herrliche");
            _replacements.Add("замечательных", "herrlichen");
            _replacements.Add("замечательный", "herrlicher");
            _replacements.Add("замечательна", "herrliche");
            _replacements.Add("замечательная", "herrliche");

            _replacements.Add("восхитительно", "wunderbar");
            _replacements.Add("восхитителен", "wunderbarer");
            _replacements.Add("восхитительные", "wunderbare");
            _replacements.Add("восхитительных", "wunderbaren");
            _replacements.Add("восхитительный", "wunderbarer");
            _replacements.Add("восхитительна", "wunderbare");
            _replacements.Add("восхитительная", "wunderbare");

            _replacements.Add("прекрасно", "schön");
            _replacements.Add("прекрасен", "schöner");
            _replacements.Add("прекрасные", "schöne");
            _replacements.Add("прекрасных", "schönen");
            _replacements.Add("прекрасный", "schöner");
            _replacements.Add("прекрасна", "schöne");
            _replacements.Add("прекрасная", "schöne");

            _replacements.Add("ассистент", "assistent");
            _replacements.Add("ассистуха", "assistent");

            _replacements.Add("свинья", "schwein");
            _replacements.Add("свинье", "schwein");
            _replacements.Add("свиней", "schwein");
            _replacements.Add("свиньи", "schwein");

            _replacements.Add("спасибо", "danke");
            _replacements.Add("спасибо большое", "danke schön");
            _replacements.Add("большое спасибо", "danke vielmals");

            _replacements.Add("женщина", "frau");
            _replacements.Add("эй", "hey");
            _replacements.Add("человек", "mensch");

            _replacements.Add("привет", "hallo");
            _replacements.Add("здравствуйте", "guten tag");
            _replacements.Add("доброе утро", "guten morgen");
            _replacements.Add("добрый день", "guten tag");
            _replacements.Add("добрый вечер", "guten abend");
            _replacements.Add("доброй ночи", "gute nacht");

            _replacements.Add("пока", "tschüss");
            _replacements.Add("прощай", "tschüss");
            _replacements.Add("прощайте", "tschüss");
            _replacements.Add("до свидания", "auf wiedersehen");

            _replacements.Add("клоун", "clown");
            _replacements.Add("клоуну", "clown");
            _replacements.Add("клоуна", "clown");
            _replacements.Add("клоуны", "clown");
            _replacements.Add("клоунов", "clown");
            _replacements.Add("клуня", "clown");
            _replacements.Add("клуне", "clown");
            _replacements.Add("клуни", "clown");
            _replacements.Add("клунь", "clown");

            _replacements.Add("поехали", "los");
            _replacements.Add("пошли", "los");
            _replacements.Add("давай", "los");
            _replacements.Add("идем", "los");
            _replacements.Add("вперед", "los");
            _replacements.Add("идём", "los");
            _replacements.Add("вперёд", "los");

            _replacements.Add("вульпы", "mannhund");
            _replacements.Add("вульп", "mannhund");
            _replacements.Add("вульпа", "mannhund");
            _replacements.Add("вульпу", "mannhund");
            _replacements.Add("вульпе", "mannhund");

            _replacements.Add("истребить", "vertilgen");
            _replacements.Add("сжечь", "verbrennen");

            _replacements.Add("убить", "töten");
            _replacements.Add("убил", "töten");
            _replacements.Add("убили", "töten");
            _replacements.Add("убью", "töten");
            _replacements.Add("убила", "töten");
            _replacements.Add("убьют", "töten");
            _replacements.Add("убейте", "töten");

            _replacements.Add("пиво", "bier");
            _replacements.Add("пива", "bier");

            _replacements.Add("вода", "wasser");
            _replacements.Add("воды", "wasser");

            _replacements.Add("агент", "agent");
            _replacements.Add("агенту", "zum agenten");
            _replacements.Add("агента", "agent");
            _replacements.Add("агенты", "agenten");
            _replacements.Add("агентов", "agenten");

            _replacements.Add("яо", "nukleare agenten");
            _replacements.Add("ядерные оперативники", "nukleare agenten");

            _replacements.Add("опер", "einsatzkräfte");
            _replacements.Add("оперу", "einsatzkräfte");
            _replacements.Add("опера", "einsatzkräfte");
            _replacements.Add("оперы", "einsatzkräfte");
            _replacements.Add("оперов", "einsatzkräfte");

            _replacements.Add("террорист", "terrorist");
            _replacements.Add("террористы", "terroristen");
            _replacements.Add("террориста", "terrorist");

            _replacements.Add("корпорация", "korporation");
            _replacements.Add("корпорации", "korporation");
            _replacements.Add("корпораций", "korporation");

            // Командование

            _replacements.Add("капитан", "führer");
            _replacements.Add("капитана", "führer'a");
            _replacements.Add("капитану", "führer");
            _replacements.Add("кеп", "führer");
            _replacements.Add("кепа", "führer'a");
            _replacements.Add("кепу", "führer");
            _replacements.Add("кэп", "führer");
            _replacements.Add("кэпа", "führer'a");
            _replacements.Add("кэпу", "führer");

            _replacements.Add("си", "chief");
            _replacements.Add("гв", "chefarzt");
            _replacements.Add("нр", "forschungsleiter");
            _replacements.Add("гп", "leiter des personals");
            _replacements.Add("гсб", "leiter des sicherheitsdienstes");
            _replacements.Add("км", "quartiermeister");

            // Служба Безопасности

            _replacements.Add("сб", "polizei");
            _replacements.Add("сбух", "sicherheit");
            _replacements.Add("сбуха", "sicherheit");
            _replacements.Add("сбухе", "sicherheit");
            _replacements.Add("сбуху", "sicherheit");
            _replacements.Add("сбухи", "sicherheit");
            _replacements.Add("сбшник", "sicherheitsbeamter");
            _replacements.Add("сбшника", "sicherheitsbeamter");
            _replacements.Add("сбшнику", "zum sicherheitsbeamten");
            _replacements.Add("сбшники", "sicherheitsbeamte");
            _replacements.Add("сбшников", "sicherheitsbeamte");

            _replacements.Add("кадет", "kadett");
            _replacements.Add("кадеты", "kadetten");
            _replacements.Add("кадета", "kadett");
            _replacements.Add("кадету", "kadett");
            _replacements.Add("кадетов", "kadett");
            _replacements.Add("кадетик", "kadett");
            _replacements.Add("кадетики", "kadetten");
            _replacements.Add("кадетика", "kadett");
            _replacements.Add("кадетику", "kadett");
            _replacements.Add("кадетиков", "kadett");

            _replacements.Add("офицер", "offizier");
            _replacements.Add("офицеры", "offizier");
            _replacements.Add("офицера", "offizier");
            _replacements.Add("офицеру", "offizier");
            _replacements.Add("офицеров", "offizier");

            // Центральное Командование

            _replacements.Add("авд", "interner affairs agent");
            _replacements.Add("магистрат", "magistrat");
            _replacements.Add("магистрату", "zum magistrat");
            _replacements.Add("магистрата", "magistrat");
            _replacements.Add("магистраты", "magistrate");
            _replacements.Add("магистратов", "magistrate");

            _replacements.Add("осщ", "blauer schild offizier");
            _replacements.Add("цк", "zentrales kommando");

            // Ругательства

            _replacements.Add("урод", "monster");
            _replacements.Add("урода", "monster");
            _replacements.Add("уроды", "monster");
            _replacements.Add("уроду", "zum monster");
            _replacements.Add("уродов", "monster");
            _replacements.Add("уродина", "monster");
            _replacements.Add("уродины", "monster");
            _replacements.Add("уродине", "zum monster");
            _replacements.Add("уродин", "monster");
            _replacements.Add("чудище", "monster");
            _replacements.Add("чудища", "monster");
            _replacements.Add("чудищу", "zum monster");
            _replacements.Add("чудищей", "monster");
            _replacements.Add("чудовище", "monster");
            _replacements.Add("чудовища", "monster");
            _replacements.Add("чудовищу", "zum monster");
            _replacements.Add("чудовищей", "monster");

            _replacements.Add("чёрт", "verdammt");
            _replacements.Add("черт", "verdammt");
            _replacements.Add("чертям", "verdammt");
            _replacements.Add("блядь", "scheiße");
            _replacements.Add("мудак", "arschloch");

            _replacements.Add("ублюдок", "bastard");
            _replacements.Add("ублюдки", "bastarde");
            _replacements.Add("ублюдка", "bastard");
            _replacements.Add("ублюдку", "zum bastard");
            _replacements.Add("ублюдков", "bastarde");

            _replacements.Add("ахуенно", "scheiß geil");
            _replacements.Add("охуенно", "scheiß geil");
            _replacements.Add("ахуительно", "scheiß geil");
            _replacements.Add("охуительно", "scheiß geil");
            _replacements.Add("зашибись", "scheiß geil");
            _replacements.Add("пиздато", "scheiß geil");
            _replacements.Add("заебись", "scheiß geil");

            _replacements.Add("пошел нахуй", "leck mich");
            _replacements.Add("пошли нахуй", "leck mich");
            _replacements.Add("пошёл нахуй", "leck mich");
            _replacements.Add("иди нахуй", "leck mich");
            _replacements.Add("идите нахуй", "leck mich");
            _replacements.Add("пошел ты нахуй", "leck mich");
            _replacements.Add("пошли вы нахуй", "leck mich");
            _replacements.Add("пошёл ты нахуй", "leck mich");
            _replacements.Add("иди ты нахуй", "leck mich");
            _replacements.Add("идите вы нахуй", "leck mich");
            _replacements.Add("съеби", "leck mich");
            _replacements.Add("съебите", "leck mich");
            _replacements.Add("съебись", "leck mich");
            _replacements.Add("съебитесь", "leck mich");

            _replacements.Add("блять", "scheiße");
            _replacements.Add("бля", "scheiße");
            _replacements.Add("бляха", "scheiße");

            _replacements.Add("сука", "hündin");
            _replacements.Add("суку", "hündin");
            _replacements.Add("суки", "hündin");
            _replacements.Add("сук", "hündin");
            _replacements.Add("суке", "hündin");
            _replacements.Add("сучка", "hündin");

            _replacements.Add("идиот", "dummkopf");
            _replacements.Add("идиоты", "dummkopf");
            _replacements.Add("идиота", "dummkopf");
            _replacements.Add("идиоту", "dummkopf");
            _replacements.Add("идиотов", "dummkopf");

            _replacements.Add("пидор", "arschloch");
            _replacements.Add("пидоры", "arschloch");
            _replacements.Add("пидора", "arschloch");
            _replacements.Add("пидору", "arschloch");
            _replacements.Add("пидоров", "arschloch");
            _replacements.Add("пидорас", "schwuchtel");
            _replacements.Add("пидорасы", "schwuchtel");
            _replacements.Add("пидораса", "schwuchtel");
            _replacements.Add("пидорасу", "schwuchtel");
            _replacements.Add("пидорасов", "schwuchtel");

            _replacements.Add("мразь", "dreckskerl");
            _replacements.Add("мразе", "dreckskerl");
            _replacements.Add("мрази", "dreckskerl");

            _replacements.Add("мерзавец", "schurke");
            _replacements.Add("мерзавцев", "schurken");
            _replacements.Add("мерзавцы", "schurken");
            _replacements.Add("мерзавца", "schurke");
            _replacements.Add("мерзавцу", "zum schurke");
            _replacements.Add("мерзавцов", "schurken");

            _replacements.Add("еблан", "trottel");
            _replacements.Add("ебланы", "trottel");
            _replacements.Add("еблана", "trottel");
            _replacements.Add("еблану", "trottel");
            _replacements.Add("ебланов", "trottel");
            _replacements.Add("ебланище", "trottel");
            _replacements.Add("ебланища", "trottel");

            _replacements.Add("уебок", "wichser");
            _replacements.Add("уёбок", "wichser");
            _replacements.Add("уебка", "wichser");
            _replacements.Add("уёбка", "wichser");
            _replacements.Add("уебки", "wichser");
            _replacements.Add("уёбки", "wichser");
            _replacements.Add("уебков", "wichser");
            _replacements.Add("уёбков", "wichser");
            _replacements.Add("уебку", "wichser");
            _replacements.Add("уёбку", "wichser");

            _replacements.Add("похуй", "scheißegal");
            _replacements.Add("поебать", "scheißegal");
            _replacements.Add("похую", "scheißegal");
            _replacements.Add("похер", "scheißegal");
            _replacements.Add("плевать", "egal");

            _replacements.Add("нахера", "zum teufel");
            _replacements.Add("нахуя", "zum teufel");

            _replacements.Add("ахуеть", "scheiße");
            _replacements.Add("охуеть", "scheiße");
            _replacements.Add("ахуй", "scheiße");

            _replacements.Add("хуйня", "scheiße");

            var orderedKeys = _replacements.Keys.OrderByDescending(k => k.Length).ToList();
            var pattern = @"\b(" + string.Join("|", orderedKeys.Select(Regex.Escape)) + @")\b";
            _replaceRegex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);

            var orderedPronounceKeys = _pronunciations.Keys.OrderByDescending(k => k.Length).ToList();
            var pronouncePattern = @"\b(" + string.Join("|", orderedPronounceKeys.Select(Regex.Escape)) + @")\b";
            _pronounceRegex = new Regex(pronouncePattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);

            SubscribeLocalEvent<DeutschAccentComponent, AccentGetEvent>(OnAccent);
        }

        public string GetPronunciation(string message)
        {
            if (_pronounceRegex == null)
                return message;

            return _pronounceRegex.Replace(message, match =>
            {
                string key = match.Value.ToLowerInvariant();
                if (_pronunciations.TryGetValue(key, out var pron))
                {
                    return pron;
                }
                return match.Value;
            });
        }

        public string Accentuate(string message)
        {
            message = ReplacePunctuation(message);
            return message;
        }

        private string ReplacePunctuation(string message)
        {
            var sentences = AccentSystem.SentenceRegex.Split(message);
            var msg = new StringBuilder();
            foreach (var s in sentences)
            {
                var toInsert = new StringBuilder();
                if (toInsert.Length == 0)
                {
                    msg.Append(s);
                }
                else
                {
                    msg.Append(s.Insert(s.Length - s.TrimStart().Length, toInsert.ToString()));
                }
            }
            return msg.ToString();
        }

        private void OnAccent(EntityUid uid, DeutschAccentComponent component, AccentGetEvent args)
        {
            var message = args.Message;

            if (_replaceRegex == null)
            {
                args.Message = Accentuate(message);
                return;
            }

            message = _replaceRegex.Replace(message, match =>
            {
                string matchedText = match.Value;
                string key = matchedText.ToLowerInvariant();
                if (!_replacements.TryGetValue(key, out var baseRep))
                    return matchedText;

                bool allUpper = true;
                bool restAreLower = true;
                bool firstIsUpper = false;

                if (matchedText.Length > 0)
                {
                    firstIsUpper = Char.IsUpper(matchedText[0]);
                    allUpper = firstIsUpper;

                    for (int i = 1; i < matchedText.Length; i++)
                    {
                        char c = matchedText[i];
                        if (Char.IsLetter(c))
                        {
                            if (Char.IsLower(c))
                            {
                                allUpper = false;
                            }
                            else
                            {
                                restAreLower = false;
                            }
                        }
                    }
                }

                bool titleCase = firstIsUpper && restAreLower;

                string rep = baseRep;
                if (allUpper)
                {
                    rep = baseRep.ToUpperInvariant();
                }
                else if (titleCase && baseRep.Length > 0)
                {
                    rep = Char.ToUpperInvariant(baseRep[0]) + baseRep.Substring(1);
                }

                return rep;
            });

            message = Accentuate(message);

            args.Message = message;
        }
    }
}
