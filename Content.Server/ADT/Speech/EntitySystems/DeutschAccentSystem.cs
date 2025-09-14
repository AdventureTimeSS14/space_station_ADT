using System.Text.RegularExpressions;
using Content.Server.Speech.Components;
//ADT-Tweak-Start
using System.Text;
using System.Globalization;
using System.Linq;
//ADT-Tweak-End

namespace Content.Server.Speech.EntitySystems
{
    public sealed class DeutchAccentSystem : EntitySystem
    {
        private readonly Dictionary<string, string> _replacements = new();
        private Regex? _replaceRegex;

        // Инструкции для TTS-а, чтобы слова произносились правильно
        private readonly Dictionary<string, string> _pronunciations = new()
        {
            {"para qué", "пара кэ"},
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
            _replacements.Add("мне", "me");
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
            _replacements.Add("моей", "mein");
            _replacements.Add("моих", "mein");
            _replacements.Add("моего", "mein");

            _replacements.Add("пожалуйста", "bitte sehr");
            _replacements.Add("ну пожалуйста", "bitte, bitte sehr");

            _replacements.Add("простите", "perdón");
            _replacements.Add("извините", "perdón");
            _replacements.Add("прошу прощение", "perdón");
            _replacements.Add("прошу прощения", "perdón");

            _replacements.Add("друг", "amigo");
            _replacements.Add("другу", "al amigo");
            _replacements.Add("друга", "amigo");
            _replacements.Add("друзья", "amigos");
            _replacements.Add("друзей", "amigos");
            _replacements.Add("дружок", "amiguito");

            _replacements.Add("подруга", "amiga");
            _replacements.Add("подруге", "a la amiga");
            _replacements.Add("подругу", "amiga");
            _replacements.Add("подруги", "amigas");
            _replacements.Add("подруг", "amigas");
            _replacements.Add("подружка", "amiguita");

            _replacements.Add("товарищ", "compañero");
            _replacements.Add("товарищу", "al compañero");
            _replacements.Add("товарища", "compañero");
            _replacements.Add("товарищи", "compañeros");
            _replacements.Add("товарищей", "compañeros");

            _replacements.Add("брат", "hermano");
            _replacements.Add("брату", "al hermano");
            _replacements.Add("брата", "hermano");
            _replacements.Add("братья", "hermanos");
            _replacements.Add("братьев", "hermanos");

            _replacements.Add("сестра", "hermana");
            _replacements.Add("сестре", "a la hermana");
            _replacements.Add("сестры", "hermana");
            _replacements.Add("сёстры", "hermanas");
            _replacements.Add("сестёр", "hermanas");

            _replacements.Add("отец", "padre");
            _replacements.Add("отцу", "al padre");
            _replacements.Add("отца", "padre");
            _replacements.Add("отцы", "padres");
            _replacements.Add("отцов", "padres");

            _replacements.Add("хорошо", "gut");
            _replacements.Add("хорош", "bueno");
            _replacements.Add("хороша", "buena");
            _replacements.Add("хороший", "bueno");
            _replacements.Add("хорошая", "buena");
            _replacements.Add("хороши", "buenos");
            _replacements.Add("хороших", "buenos");
            _replacements.Add("хорошего", "buenos");

            _replacements.Add("отлично", "ausgezeichnet");

            _replacements.Add("великолепно", "magníficamente");
            _replacements.Add("великолепен", "magnífico");
            _replacements.Add("великолепные", "magníficos");
            _replacements.Add("великолепных", "magníficos");
            _replacements.Add("великолепный", "magnífico");
            _replacements.Add("великолепна", "magnífica");
            _replacements.Add("великолепная", "magnífica");

            _replacements.Add("замечательно", "estupendo");
            _replacements.Add("замечателен", "estupendo");
            _replacements.Add("замечательные", "estupendos");
            _replacements.Add("замечательных", "estupendos");
            _replacements.Add("замечательный", "estupendo");
            _replacements.Add("замечательна", "estupendo");
            _replacements.Add("замечательная", "estupendo");

            _replacements.Add("восхитительно", "wunderschoen");
            _replacements.Add("восхитителен", "maravilloso");
            _replacements.Add("восхитительные", "maravillosos");
            _replacements.Add("восхитительных", "maravillosos");
            _replacements.Add("восхитительный", "maravilloso");
            _replacements.Add("восхитительна", "maravillosa");
            _replacements.Add("восхитительная", "maravillosa");

            _replacements.Add("прекрасно", "schon");
            _replacements.Add("прекрасен", "hermoso");
            _replacements.Add("прекрасные", "hermosos");
            _replacements.Add("прекрасных", "hermosos");
            _replacements.Add("прекрасный", "hermoso");
            _replacements.Add("прекрасна", "hermosa");
            _replacements.Add("прекрасная", "hermosa");

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

            _replacements.Add("пока", "adiós");
            _replacements.Add("прощай", "adiós");
            _replacements.Add("прощайте", "adiós a todos");
            _replacements.Add("до свидания", "hasta la vista");

            _replacements.Add("клоун", "clown");
            _replacements.Add("клоуну", "clown");
            _replacements.Add("клоуна", "clown");
            _replacements.Add("клоуны", "clown");
            _replacements.Add("клоунов", "clown");
            _replacements.Add("клуня", "clown");
            _replacements.Add("клуне", "clown");
            _replacements.Add("клуни", "clown");
            _replacements.Add("клунь", "clown");

            _replacements.Add("поехали", "vamos");
            _replacements.Add("пошли", "vamos");
            _replacements.Add("давай", "vamos");
            _replacements.Add("идем", "vamos");
            _replacements.Add("вперед", "vamos");
            _replacements.Add("идём", "vamos");
            _replacements.Add("вперёд", "vamos");

            _replacements.Add("вульпы", "mann hund");
            _replacements.Add("вульп", "mann hund");
            _replacements.Add("вульпа", "mann hund");
            _replacements.Add("вульпу", "mann hund");
            _replacements.Add("вульпе", "mann hund");

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

            _replacements.Add("агент", "agente");
            _replacements.Add("агенту", "al agente");
            _replacements.Add("агента", "agente");
            _replacements.Add("агенты", "agentes");
            _replacements.Add("агентов", "agentes");

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
            _replacements.Add("нр", "doktorvater");
            _replacements.Add("гп", "leiter des personals");
            _replacements.Add("гсб", "leiter des sicherheitsdienstes");
            _replacements.Add("км", "quartiermeister");

            // Служба Безопасности

            _replacements.Add("сб", "polizei");
            _replacements.Add(" сб", " de seguridad");
            _replacements.Add("сбух", "seguras");
            _replacements.Add("сбуха", "segura");
            _replacements.Add("сбухе", "al segura");
            _replacements.Add("сбуху", "segura");
            _replacements.Add("сбухи", "seguras");
            _replacements.Add("сбшник", "segurata");
            _replacements.Add("сбшника", "segurata");
            _replacements.Add("сбшнику", "al segurata");
            _replacements.Add("сбшники", "seguratas");
            _replacements.Add("сбшников", "seguratas");

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

            _replacements.Add("авд", "agente de asuntos internos");

            _replacements.Add("магистрат", "magistrado");
            _replacements.Add("магистрату", "al magistrado");
            _replacements.Add("магистрата", "magistrado");
            _replacements.Add("магистраты", "magistrados");
            _replacements.Add("магистратов", "magistrados");

            _replacements.Add("осщ", "oficial del escudo azul");
            _replacements.Add("цк", "comando central");

            // Ругательства

            _replacements.Add("урод", "monstruo");
            _replacements.Add("урода", "monstruo");
            _replacements.Add("уроды", "monstruos");
            _replacements.Add("уроду", "al monstruo");
            _replacements.Add("уродов", "monstruos");
            _replacements.Add("уродина", "monstruo");
            _replacements.Add("уродины", "monstruos");
            _replacements.Add("уродине", "al monstruo");
            _replacements.Add("уродин", "monstruos");
            _replacements.Add("чудище", "monstruo");
            _replacements.Add("чудища", "monstruos");
            _replacements.Add("чудищу", "al monstruo");
            _replacements.Add("чудищей", "monstruos");
            _replacements.Add("чудовище", "monstruo");
            _replacements.Add("чудовища", "monstruos");
            _replacements.Add("чудовищу", "al monstruo");
            _replacements.Add("чудовищей", "monstruos");

            _replacements.Add("чёрт", "puta madre");
            _replacements.Add("черт", "puta madre");
            _replacements.Add("чертям", "puta madre");
            _replacements.Add("блядь", "puta madre");
            _replacements.Add("мудак", "puta madre");

            _replacements.Add("ублюдок", "bastardo");
            _replacements.Add("ублюдки", "bastardos");
            _replacements.Add("ублюдка", "bastardo");
            _replacements.Add("ублюдку", "al bastardo");
            _replacements.Add("ублюдков", "bastardos");

            _replacements.Add("ахуенно", "de puta madre");
            _replacements.Add("охуенно", "de puta madre");
            _replacements.Add("ахуительно", "de puta madre");
            _replacements.Add("охуительно", "de puta madre");
            _replacements.Add("зашибись", "de puta madre");
            _replacements.Add("пиздато", "de puta madre");
            _replacements.Add("заебись", "de puta madre");

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

            _replacements.Add("блять", "scheibe");
            _replacements.Add("бля", "scheibe");
            _replacements.Add("бляха", "scheibe");

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

            _replacements.Add("мерзавец", "canalla");
            _replacements.Add("мерзавцев", "canallas");
            _replacements.Add("мерзавцы", "canallas");
            _replacements.Add("мерзавца", "canalla");
            _replacements.Add("мерзавцу", "al canalla");
            _replacements.Add("мерзавцов", "canallas");

            _replacements.Add("еблан", "ficker");
            _replacements.Add("ебланы", "ficker");
            _replacements.Add("еблана", "ficker");
            _replacements.Add("еблану", "ficker");
            _replacements.Add("ебланов", "ficker");
            _replacements.Add("ебланище", "scheißkerl");
            _replacements.Add("ебланища", "scheißkerl");

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

            _replacements.Add("похуй", "scheib");
            _replacements.Add("поебать", "scheib");
            _replacements.Add("похую", "scheib");
            _replacements.Add("похер", "importa un bledo");
            _replacements.Add("плевать", "ist mir egal");

            _replacements.Add("нахера", "fick dich");
            _replacements.Add("нахуя", "fick dich");

            _replacements.Add("ахуеть", "hostia puta");
            _replacements.Add("охуеть", "hostia puta");
            _replacements.Add("ахуй", "hostia puta");

            _replacements.Add("хуйня", "mierda");

            var orderedKeys = _replacements.Keys.OrderByDescending(k => k.Length).ToList();
            var pattern = @"\b(" + string.Join("|", orderedKeys.Select(Regex.Escape)) + @")\b";
            _replaceRegex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);

            var orderedPronounceKeys = _pronunciations.Keys.OrderByDescending(k => k.Length).ToList();
            var pronouncePattern = @"\b(" + string.Join("|", orderedPronounceKeys.Select(Regex.Escape)) + @")\b";
            _pronounceRegex = new Regex(pronouncePattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);

            SubscribeLocalEvent<SpanishAccentComponent, AccentGetEvent>(OnAccent);
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

        private void OnAccent(EntityUid uid, SpanishAccentComponent component, AccentGetEvent args)
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
