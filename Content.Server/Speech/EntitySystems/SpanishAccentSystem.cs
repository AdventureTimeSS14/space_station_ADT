using System.Text;
using Content.Server.Speech.Components;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Linq;

namespace Content.Server.Speech.EntitySystems
{
    public sealed class SpanishAccentSystem : EntitySystem
    {
        private readonly Dictionary<string, string> _replacements = new();
        private Regex? _replaceRegex;

        // New field for TTS pronunciations
        private readonly Dictionary<string, string> _pronunciations = new()
        {
            {"para que", "пара кэ"},
            {"por que", "пор кэ"},
            {"como", "комо"},
            {"asi", "аси"},
            {"me", "мэ"},
            {"por favor", "пор фавор"},
            {"por favor, hazlo", "пор фавор, асло"},
            {"amigo", "амиго"},
            {"al amigo", "аль амиго"},
            {"amigos", "амигос"},
            {"amiguito", "амигито"},
            {"amiga", "амига"},
            {"a la amiga", "а ла амига"},
            {"amigas", "амигас"},
            {"amiguita", "амигита"},
            {"companero", "компаньэро"},
            {"al companero", "аль компанэеро"},
            {"companeros", "компаньэрос"},
            {"hermano", "эрмано"},
            {"al hermano", "аль эрмано"},
            {"hermanos", "эрманос"},
            {"hermana", "эрмана"},
            {"al hermana", "аль эрмана"},
            {"hermanas", "эрманас"},
            {"padre", "падрэ"},
            {"al padre", "аль падрэ"},
            {"padres", "падрэс"},
            {"bien", "бьен"},
            {"bueno", "буэно"},
            {"buena", "буэна"},
            {"buenos", "буэнос"},
            {"excelente", "эхсэлентэ"},
            {"magnificamente", "магнификаментэ"},
            {"magnifico", "магнифико"},
            {"magnificos", "магнификос"},
            {"magnifica", "магнифика"},
            {"estupendo", "эступендо"},
            {"estupendos", "эступендос"},
            {"maravillosamente", "маравийосаментэ"},
            {"maravilloso", "маравийосо"},
            {"maravillosos", "маравийосос"},
            {"maravillosa", "маравийоса"},
            {"hermosamente", "эрмосаментэ"},
            {"hermoso", "эрмосо"},
            {"hermosos", "эрмосос"},
            {"hermosa", "эрмоса"},
            {"asistente", "асистентэ"},
            {"ayudante", "айудантэ"},
            {"cerdo", "сэрдо"},
            {"cerdos", "сэрдос"},
            {"gracias", "грасиас"},
            {"mujer", "мухэр"},
            {"oye", "ойэ"},
            {"persona", "персона"},
            {"hola", "ола"},
            {"buenos dias", "буэнос диас"},
            {"buenas tardes", "буэнас тардэс"},
            {"buenas noches", "буэнас ночэс"},
            {"adios", "адиос"},
            {"adios a todos", "адиос а тодос"},
            {"hasta la vista", "аста ла виста"},
            {"payaso", "пайасо"},
            {"al payaso", "аль пайасо"},
            {"payasos", "пайасос"},
            {"payasito", "пайасито"},
            {"al payasito", "аль пайасито"},
            {"payasitos", "пайаситос"},
            {"vamos", "вамос"},
            {"zorras", "соррас"},
            {"zorro", "сорро"},
            {"zorra", "сорра"},
            {"al zorra", "аль сорра"},
            {"exterminar", "экстерминар"},
            {"cerveza", "сэрвэса"},
            {"agua", "агуа"},
            {"agente", "ахентэ"},
            {"al agente", "аль ахентэ"},
            {"agentes", "ахентэс"},
            {"operativos nucleares", "оперативос нуклеарэс"},
            {"operativo", "оперативо"},
            {"al operativo", "аль оперативо"},
            {"operativos", "оперативос"},
            {"terrorista", "террориста"},
            {"terroristas", "террористас"},
            {"corporacion", "корпорасьон"},
            {"corporaciones", "корпорасьонэс"},
            {"capitan", "капитан"},
            {"al capitan", "аль капитан"},
            {"jefe ingeniero", "хэфэ инхэньэро"},
            {"jefe medico", "хэфэ мэдико"},
            {"director cientifico", "директор сиэнтифико"},
            {"jefe de personal", "хэфэ дэ персонал"},
            {"jefe de seguridad", "хэфэ дэ сэгуридад"},
            {"intendente", "интендэнтэ"},
            {"policia", "полисиа"},
            {"de seguridad", "дэ сэгуридад"},
            {"seguras", "сэгурас"},
            {"segura", "сэгура"},
            {"al segura", "аль сэгура"},
            {"segurata", "сэгурата"},
            {"al segurata", "аль сэгурата"},
            {"seguratas", "сэгуратас"},
            {"cadete", "кадэтэ"},
            {"cadetes", "кадэтэс"},
            {"al cadete", "аль кадэтэ"},
            {"oficial", "офисиал"},
            {"oficiales", "офисиалэс"},
            {"al oficial", "аль офисиал"},
            {"agente de asuntos internos", "ахентэ дэ асунтос интернос"},
            {"magistrado", "магистрадо"},
            {"al magistrado", "аль магистрадо"},
            {"magistrados", "магистрадос"},
            {"oficial del escudo azul", "офисиал дэль эскудо асуль"},
            {"comando central", "командо сэнтрал"},
            {"puta madre", "пута мадрэ"},
            {"de puta madre", "дэ пута мадрэ"},
            {"vete a la mierda", "вэтэ а ла мьерда"},
            {"vayanse a la mierda", "байансэ а ла мьерда"},
            {"mierda", "мьерда"},
            {"perra", "пэрра"},
            {"perras", "пэррас"},
            {"al perra", "аль пэрра"},
            {"perrita", "пэррита"},
            {"idiota", "идиота"},
            {"idiotas", "идиотас"},
            {"al idiota", "аль идиота"},
            {"cabron", "каброн"},
            {"cabrones", "кабронэс"},
            {"al cabron", "аль каброн"},
            {"maricon", "марикон"},
            {"maricones", "мариконэс"},
            {"al maricon", "аль марикон"},
            {"canalla", "канайя"},
            {"canallas", "канайяс"},
            {"gilipollas", "хилипойяс"},
            {"al gilipollas", "аль хилипойяс"},
            {"hijo de puta", "ихо дэ пута"},
            {"hijos de puta", "ихос дэ пута"},
            {"al hijos de puta", "аль ихос дэ пута"},
            {"importa un carajo", "импорта ун карахо"},
            {"importa un bledo", "импорта ун блэдо"},
            {"no me importa", "но мэ импорта"},
            {"para que mierda", "пара кэ мьерда"},
            {"hostia puta", "остиа пута"}
        };

        private Regex? _pronounceRegex;

        public override void Initialize()
        {
            base.Initialize();

            _replacements.Add("зачем", "para que");
            _replacements.Add("почему", "por que");
            _replacements.Add("как", "como");
            _replacements.Add("так", "asi");
            _replacements.Add("мне", "me");

            _replacements.Add("пожалуйста", "por favor");
            _replacements.Add("ну пожалуйста", "por favor, hazlo");

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

            _replacements.Add("товарищ", "companero");
            _replacements.Add("товарищу", "al companero");
            _replacements.Add("товарища", "companero");
            _replacements.Add("товарищи", "companeros");
            _replacements.Add("товарищей", "companeros");

            _replacements.Add("брат", "hermano");
            _replacements.Add("брату", "al hermano");
            _replacements.Add("брата", "hermano");
            _replacements.Add("братья", "hermanos");
            _replacements.Add("братьев", "hermanos");

            _replacements.Add("сестра", "hermana");
            _replacements.Add("сестре", "al hermana");
            _replacements.Add("сестры", "hermana");
            _replacements.Add("сёстры", "hermanas");
            _replacements.Add("сестёр", "hermanas");

            _replacements.Add("отец", "padre");
            _replacements.Add("отцу", "al padre");
            _replacements.Add("отца", "padre");
            _replacements.Add("отцы", "padres");
            _replacements.Add("отцов", "padres");

            _replacements.Add("хорошо", "bien");
            _replacements.Add("хорош", "bueno");
            _replacements.Add("хороша", "buena");
            _replacements.Add("хороший", "bueno");
            _replacements.Add("хорошая", "buena");
            _replacements.Add("хороши", "buenos");
            _replacements.Add("хороших", "buenos");
            _replacements.Add("хорошего", "buenos");

            _replacements.Add("отлично", "excelente");

            _replacements.Add("великолепно", "magnificamente");
            _replacements.Add("великолепен", "magnifico");
            _replacements.Add("великолепные", "magnificos");
            _replacements.Add("великолепных", "magnificos");
            _replacements.Add("великолепный", "magnifico");
            _replacements.Add("великолепна", "magnifica");
            _replacements.Add("великолепная", "magnifica");

            _replacements.Add("замечательно", "estupendo");
            _replacements.Add("замечателен", "estupendo");
            _replacements.Add("замечательные", "estupendos");
            _replacements.Add("замечательных", "estupendos");
            _replacements.Add("замечательный", "estupendo");
            _replacements.Add("замечательна", "estupendo");
            _replacements.Add("замечательная", "estupendo");

            _replacements.Add("восхитительно", "maravillosamente");
            _replacements.Add("восхитителен", "maravilloso");
            _replacements.Add("восхитительные", "maravillosos");
            _replacements.Add("восхитительных", "maravillosos");
            _replacements.Add("восхитительный", "maravilloso");
            _replacements.Add("восхитительна", "maravillosa");
            _replacements.Add("восхитительная", "maravillosa");

            _replacements.Add("прекрасно", "hermosamente");
            _replacements.Add("прекрасен", "hermoso");
            _replacements.Add("прекрасные", "hermosos");
            _replacements.Add("прекрасных", "hermosos");
            _replacements.Add("прекрасный", "hermoso");
            _replacements.Add("прекрасна", "hermosa");
            _replacements.Add("прекрасная", "hermosa");

            _replacements.Add("ассистент", "asistente");
            _replacements.Add("ассистуха", "ayudante");

            _replacements.Add("свинья", "cerdo");
            _replacements.Add("свинье", "cerdo");
            _replacements.Add("свиней", "cerdos");
            _replacements.Add("свиньи", "cerdos");

            _replacements.Add("спасибо", "gracias");
            _replacements.Add("женщина", "mujer");
            _replacements.Add("эй", "oye");
            _replacements.Add("человек", "persona");

            _replacements.Add("привет", "hola");
            _replacements.Add("здравствуйте", "hola");
            _replacements.Add("доброе утро", "buenos dias");
            _replacements.Add("добрый вечер", "buenas tardes");
            _replacements.Add("доброй ночи", "buenas noches");

            _replacements.Add("пока", "adios");
            _replacements.Add("прощай", "adios");
            _replacements.Add("прощайте", "adios a todos");
            _replacements.Add("до свидания", "hasta la vista");

            _replacements.Add("клоун", "payaso");
            _replacements.Add("клоуну", "al payaso");
            _replacements.Add("клоуна", "payaso");
            _replacements.Add("клоуны", "payasos");
            _replacements.Add("клоунов", "payasos");
            _replacements.Add("клуня", "payasito");
            _replacements.Add("клуне", "al payasito");
            _replacements.Add("клуни", "payasitos");
            _replacements.Add("клунь", "payasitos");

            _replacements.Add("поехали", "vamos");
            _replacements.Add("пошли", "vamos");
            _replacements.Add("давай", "vamos");

            _replacements.Add("вульпы", "zorras");
            _replacements.Add("вульп", "zorro");
            _replacements.Add("вульпа", "zorra");
            _replacements.Add("вульпу", "zorra");
            _replacements.Add("вульпе", "al zorra");

            _replacements.Add("истребить", "exterminar");

            _replacements.Add("пиво", "cerveza");
            _replacements.Add("пива", "cerveza");

            _replacements.Add("вода", "agua");
            _replacements.Add("воды", "agua");

            _replacements.Add("агент", "agente");
            _replacements.Add("агенту", "al agente");
            _replacements.Add("агента", "agente");
            _replacements.Add("агенты", "agentes");
            _replacements.Add("агентов", "agentes");

            _replacements.Add("яо", "operativos nucleares");
            _replacements.Add("ядерные оперативники", "operativos nucleares");

            _replacements.Add("опер", "operativo");
            _replacements.Add("оперу", "al operativo");
            _replacements.Add("опера", "operativo");
            _replacements.Add("оперы", "operativos");
            _replacements.Add("оперов", "operativos");

            _replacements.Add("террорист", "terrorista");
            _replacements.Add("террористы", "terroristas");
            _replacements.Add("террориста", "terrorista");

            _replacements.Add("корпорация", "corporacion");
            _replacements.Add("корпорации", "corporaciones");
            _replacements.Add("корпораций", "corporaciones");

            // Командование

            _replacements.Add("капитан", "capitan");
            _replacements.Add("капитана", "al capitan");
            _replacements.Add("капитану", "al capitan");
            _replacements.Add("кеп", "capitan");
            _replacements.Add("кепа", "al capitan");
            _replacements.Add("кепу", "al capitan");
            _replacements.Add("кэп", "capitan");
            _replacements.Add("кэпа", "al capitan");
            _replacements.Add("кэпу", "al capitan");

            _replacements.Add("си", "jefe ingeniero");
            _replacements.Add("гв", "jefe medico");
            _replacements.Add("нр", "director cientifico");
            _replacements.Add("гп", "jefe de personal");
            _replacements.Add("гсб", "jefe de seguridad");
            _replacements.Add("км", "intendente");

            // Служба Безопасности

            _replacements.Add("сб", "policia");
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

            _replacements.Add("кадет", "cadete");
            _replacements.Add("кадеты", "cadetes");
            _replacements.Add("кадета", "cadete");
            _replacements.Add("кадету", "al cadete");
            _replacements.Add("кадетов", "cadetes");
            _replacements.Add("кадетик", "cadete");
            _replacements.Add("кадетики", "cadetes");
            _replacements.Add("кадетика", "cadete");
            _replacements.Add("кадетику", "al cadete");
            _replacements.Add("кадетиков", "cadetes");

            _replacements.Add("офицер", "oficial");
            _replacements.Add("офицеры", "oficiales");
            _replacements.Add("офицера", "oficial");
            _replacements.Add("офицеру", "al oficial");
            _replacements.Add("офицеров", "oficiales");

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

            _replacements.Add("чёрт", "puta madre");
            _replacements.Add("черт", "puta madre");
            _replacements.Add("блядь", "puta madre");
            _replacements.Add("мудак", "puta madre");
            _replacements.Add("ублюдок", "puta madre");

            _replacements.Add("ахуенно", "de puta madre");
            _replacements.Add("охуенно", "de puta madre");
            _replacements.Add("ахуительно", "de puta madre");
            _replacements.Add("охуительно", "de puta madre");
            _replacements.Add("зашибись", "de puta madre");
            _replacements.Add("пиздато", "de puta madre");
            _replacements.Add("заебись", "de puta madre");

            _replacements.Add("пошел нахуй", "vete a la mierda");
            _replacements.Add("пошли нахуй", "vayanse a la mierda");
            _replacements.Add("пошёл нахуй", "vete a la mierda");
            _replacements.Add("иди нахуй", "vete a la mierda");
            _replacements.Add("идите нахуй", "vayanse a la mierda");
            _replacements.Add("пошел ты нахуй", "vete a la mierda");
            _replacements.Add("пошли вы нахуй", "vayanse a la mierda");
            _replacements.Add("пошёл ты нахуй", "vete a la mierda");
            _replacements.Add("иди ты нахуй", "vete a la mierda");
            _replacements.Add("идите вы нахуй", "vayanse a la mierda");
            _replacements.Add("съеби", "vete a la mierda");
            _replacements.Add("съебите", "vayanse a la mierda");
            _replacements.Add("съебись", "vete a la mierda");
            _replacements.Add("съебитесь", "vayanse a la mierda");

            _replacements.Add("блять", "mierda");
            _replacements.Add("бля", "mierda");
            _replacements.Add("бляха", "mierda");

            _replacements.Add("сука", "perra");
            _replacements.Add("суку", "perra");
            _replacements.Add("суки", "perras");
            _replacements.Add("сук", "perras");
            _replacements.Add("суке", "al perra");
            _replacements.Add("сучка", "perrita");

            _replacements.Add("идиот", "idiota");
            _replacements.Add("идиоты", "idiotas");
            _replacements.Add("идиота", "idiota");
            _replacements.Add("идиоту", "al idiota");
            _replacements.Add("идиотов", "idiotas");

            _replacements.Add("пидор", "cabron");
            _replacements.Add("пидоры", "cabrones");
            _replacements.Add("пидора", "cabron");
            _replacements.Add("пидору", "al cabron");
            _replacements.Add("пидоров", "cabrones");
            _replacements.Add("пидорас", "maricon");
            _replacements.Add("пидорасы", "maricones");
            _replacements.Add("пидораса", "maricon");
            _replacements.Add("пидорасу", "al maricon");
            _replacements.Add("пидорасов", "maricones");

            _replacements.Add("мразь", "canalla");
            _replacements.Add("мразе", "canalla");
            _replacements.Add("мрази", "canallas");

            _replacements.Add("еблан", "gilipollas");
            _replacements.Add("ебланы", "gilipollas");
            _replacements.Add("еблана", "gilipollas");
            _replacements.Add("еблану", "al gilipollas");
            _replacements.Add("ебланов", "gilipollas");
            _replacements.Add("ебланище", "gilipollas");
            _replacements.Add("ебланища", "gilipollas");

            _replacements.Add("уебок", "hijo de puta");
            _replacements.Add("уёбок", "hijo de puta");
            _replacements.Add("уебка", "hijo de puta");
            _replacements.Add("уёбка", "hijo de puta");
            _replacements.Add("уебки", "hijos de puta");
            _replacements.Add("уёбки", "hijos de puta");
            _replacements.Add("уебков", "hijos de puta");
            _replacements.Add("уёбков", "hijos de puta");
            _replacements.Add("уебку", "al hijos de puta");
            _replacements.Add("уёбку", "al hijos de puta");

            _replacements.Add("похуй", "importa un carajo");
            _replacements.Add("поебать", "importa un carajo");
            _replacements.Add("похую", "importa un carajo");
            _replacements.Add("похер", "importa un bledo");
            _replacements.Add("плевать", "no me importa");
            _replacements.Add("мне плевать", "no me importa");

            _replacements.Add("нахера", "para que mierda");
            _replacements.Add("нахуя", "para que mierda");

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
            return _pronounceRegex!.Replace(message, match =>
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
            // Insert E before every S
            message = InsertS(message);
            // If a sentence ends with ?, insert a reverse ? at the beginning of the sentence
            message = ReplacePunctuation(message);
            return message;
        }

        private string InsertS(string message)
        {
            // Replace every new Word that starts with s/S
            var msg = message.Replace(" s", " es").Replace(" S", " Es");

            // Still need to check if the beginning of the message starts
            if (msg.StartsWith("s", StringComparison.Ordinal))
            {
                return msg.Remove(0, 1).Insert(0, "es");
            }
            else if (msg.StartsWith("S", StringComparison.Ordinal))
            {
                return msg.Remove(0, 1).Insert(0, "Es");
            }

            return msg;
        }

        private string ReplacePunctuation(string message)
        {
            var sentences = AccentSystem.SentenceRegex.Split(message);
            var msg = new StringBuilder();
            foreach (var s in sentences)
            {
                var toInsert = new StringBuilder();
                for (var i = s.Length - 1; i >= 0 && "?!‽".Contains(s[i]); i--)
                {
                    toInsert.Append(s[i] switch
                    {
                        '?' => '¿',
                        '!' => '¡',
                        '‽' => '⸘',
                        _ => ' '
                    });
                }
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

            message = _replaceRegex!.Replace(message, match =>
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
