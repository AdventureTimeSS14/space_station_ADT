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

        public override void Initialize()
        {
            base.Initialize();

            _replacements.Add("зачем", "para que");
            _replacements.Add("почему", "por que");
            _replacements.Add("как", "como");
            _replacements.Add("так", "asi");
            _replacements.Add("мне", "me");

            _replacements.Add("пожалуйста", "por favor");
            _replacements.Add("ну пожалуйста", "por favor");

            _replacements.Add("друг", "amigo");
            _replacements.Add("другу", "amigo");
            _replacements.Add("друга", "amigo");
            _replacements.Add("дружок", "amiguito");
            _replacements.Add("друзья", "amigos");
            _replacements.Add("друзей", "amigos");

            _replacements.Add("подруга", "amiga");
            _replacements.Add("подруге", "amiga");
            _replacements.Add("подругу", "amiga");
            _replacements.Add("подружка", "amiguita");
            _replacements.Add("подруги", "amigas");
            _replacements.Add("подруг", "amigas");

            _replacements.Add("хорошо", "bueno");
            _replacements.Add("хороша", "buena");
            _replacements.Add("хороши", "buenos");
            _replacements.Add("хороших", "buenos");

            _replacements.Add("отлично", "excelente");

            _replacements.Add("великолепно", "magnificamente");
            _replacements.Add("великолепен", "magnifico");
            _replacements.Add("великолепные", "magnificos");
            _replacements.Add("великолепных", "magnificos");
            _replacements.Add("великолепный", "magnifico");
            _replacements.Add("великолепна", "magnifica");
            _replacements.Add("великолепная", "magnifica");

            _replacements.Add("замечательно", "maravillosamente");
            _replacements.Add("замечателен", "admirable");
            _replacements.Add("замечательные", "admirables");
            _replacements.Add("замечательных", "admirables");
            _replacements.Add("замечательный", "admirable");
            _replacements.Add("замечательна", "admirable");
            _replacements.Add("замечательная", "admirable");

            _replacements.Add("восхитительно", "maravilloso");
            _replacements.Add("восхитителен", "maravilloso");
            _replacements.Add("восхитительные", "maravilloso");
            _replacements.Add("восхитительных", "maravilloso");
            _replacements.Add("восхитительный", "maravilloso");
            _replacements.Add("восхитительна", "maravillosa");
            _replacements.Add("восхитительная", "maravillosa");

            _replacements.Add("прекрасно", "hermoso");
            _replacements.Add("прекрасен", "hermoso");
            _replacements.Add("прекрасные", "hermosos");
            _replacements.Add("прекрасных", "hermosos");
            _replacements.Add("прекрасный", "hermoso");
            _replacements.Add("прекрасна", "hermosa");
            _replacements.Add("прекрасная", "hermosa");

            _replacements.Add("ассистент", "asistente");
            _replacements.Add("ассистуха", "asistente");
            _replacements.Add("свинья", "cerdo");
            _replacements.Add("спасибо", "gracias");
            _replacements.Add("женщина", "mujer");
            _replacements.Add("эй", "oye");
            _replacements.Add("человек", "persona");

            _replacements.Add("привет", "hola");
            _replacements.Add("здравствуйте", "hola");
            _replacements.Add("доброе утро", "buenos dias");
            _replacements.Add("доброй ночи", "buenas noches");

            _replacements.Add("пока", "adios");
            _replacements.Add("прощай", "adios");
            _replacements.Add("прощайте", "adios");
            _replacements.Add("до свидания", "hasta la vista");

            _replacements.Add("клоун", "payaso");
            _replacements.Add("клоуны", "payasos");
            _replacements.Add("клоуна", "payaso");

            _replacements.Add("вульпы", "zorros");
            _replacements.Add("вульп", "zorro");
            _replacements.Add("вульпа", "zorra");
            _replacements.Add("вульпу", "zorro");
            _replacements.Add("вульпе", "zorra");

            _replacements.Add("истребить", "exterminar");

            _replacements.Add("пиво", "cerveza");
            _replacements.Add("пива", "cerveza");

            _replacements.Add("вода", "agua");
            _replacements.Add("воды", "agua");

            _replacements.Add("яо", "operativos nucleares");
            _replacements.Add("ядерные оперативники", "operativos nucleares");

            _replacements.Add("террорист", "terrorista");
            _replacements.Add("террористы", "terroristas");
            _replacements.Add("террориста", "terrorista");

            // Командование

            _replacements.Add("капитан", "capitan");
            _replacements.Add("капитана", "al capitan");
            _replacements.Add("кеп", "capitan");
            _replacements.Add("кепа", "al capitan");
            _replacements.Add("кэп", "capitan");
            _replacements.Add("кэпа", "al capitan");

            _replacements.Add("си", "jefe ingeniero");
            _replacements.Add("гв", "jefe medico");
            _replacements.Add("нр", "director cientifico");
            _replacements.Add("гп", "jefe de personal");
            _replacements.Add("гсб", "jefe de seguridad");
            _replacements.Add("км", "intendente");

            // Служба Безопасности

            _replacements.Add("сб", "de seguridad");
            _replacements.Add("сбух", "de seguridad");
            _replacements.Add("сбуха", "de seguridad");
            _replacements.Add("сбухи", "de seguridad");
            _replacements.Add("сбшник", "de seguridad");
            _replacements.Add("сбшника", "de seguridad");
            _replacements.Add("сбшники", "de seguridad");
            _replacements.Add("сбшников", "de seguridad");

            _replacements.Add("кадет", "cadete");
            _replacements.Add("кадеты", "cadetes");
            _replacements.Add("кадета", "cadete");
            _replacements.Add("кадету", "cadete");
            _replacements.Add("кадетов", "cadetes");
            _replacements.Add("кадетик", "cadete");
            _replacements.Add("кадетики", "cadetes");
            _replacements.Add("кадетика", "cadete");
            _replacements.Add("кадетику", "cadete");
            _replacements.Add("кадетиков", "cadetes");

            _replacements.Add("офицер", "oficial");
            _replacements.Add("офицеры", "oficialidad");
            _replacements.Add("офицера", "oficial");
            _replacements.Add("офицеров", "oficiales");

            // Центральное Командование

            _replacements.Add("авд", "agente de asuntos internos");

            _replacements.Add("магистрат", "magistrado");
            _replacements.Add("магистрата", "magistrado");
            _replacements.Add("магистраты", "magistrados");
            _replacements.Add("магистратов", "magistrados");

            _replacements.Add("осщ", "oficial escudo azul");
            _replacements.Add("цк", "comando central");

            // Ругательства

            _replacements.Add("пошел нахуй", "vete a la mierda");
            _replacements.Add("пошли нахуй", "vete a la mierda");
            _replacements.Add("пошёл нахуй", "vete a la mierda");
            _replacements.Add("иди нахуй", "vete a la mierda");
            _replacements.Add("идите нахуй", "vete a la mierda");
            _replacements.Add("пошел ты нахуй", "vete a la mierda");
            _replacements.Add("пошли вы нахуй", "vete a la mierda");
            _replacements.Add("пошёл ты нахуй", "vete a la mierda");
            _replacements.Add("иди ты нахуй", "vete a la mierda");
            _replacements.Add("идите вы нахуй", "vete a la mierda");

            _replacements.Add("блять", "mierda");
            _replacements.Add("бля", "mierda");
            _replacements.Add("бляха", "mierda");

            _replacements.Add("сука", "perra");
            _replacements.Add("суку", "perro");
            _replacements.Add("суки", "perros");
            _replacements.Add("сук", "perros");
            _replacements.Add("суке", "perros");
            _replacements.Add("сучка", "perrita");

            _replacements.Add("идиот", "idiota");
            _replacements.Add("идиоты", "idiota");
            _replacements.Add("идиотов", "idiotas");

            _replacements.Add("пидор", "cabron");
            _replacements.Add("пидоры", "maricones");
            _replacements.Add("пидора", "cabron");
            _replacements.Add("пидору", "maricon");
            _replacements.Add("пидоров", "maricones");
            _replacements.Add("пидорас", "maricon");
            _replacements.Add("пидорасы", "maricones");
            _replacements.Add("пидораса", "maricon");
            _replacements.Add("пидорасу", "maricon");
            _replacements.Add("пидорасов", "maricones");

            _replacements.Add("мразь", "canalla");
            _replacements.Add("мразе", "canalla");
            _replacements.Add("мрази", "canallas");

            _replacements.Add("еблан", "gilipollas");
            _replacements.Add("ебланы", "gilipollas");
            _replacements.Add("еблана", "gilipollas");
            _replacements.Add("еблану", "gilipollas");
            _replacements.Add("ебланов", "gilipollas");
            _replacements.Add("ебланище", "cabron");
            _replacements.Add("ебланища", "cabron");

            _replacements.Add("уебок", "hijo de puta");
            _replacements.Add("уёбок", "hijo de puta");
            _replacements.Add("уебка", "hijo de puta");
            _replacements.Add("уёбка", "hijo de puta");
            _replacements.Add("уебки", "hijos de puta");
            _replacements.Add("уёбки", "hijos de puta");
            _replacements.Add("уебков", "hijos de puta");
            _replacements.Add("уёбков", "hijos de puta");
            _replacements.Add("уебку", "hijos de puta");
            _replacements.Add("уёбку", "hijos de puta");

            _replacements.Add("похуй", "importa un carajo");
            _replacements.Add("поебать", "importa un carajo");
            _replacements.Add("похую", "importa un carajo");
            _replacements.Add("похер", "importa un carajo");

            _replacements.Add("нахера", "para que mierda");
            _replacements.Add("нахуя", "para que mierda");

            _replacements.Add("хуйня", "mierda");

            var orderedKeys = _replacements.Keys.OrderByDescending(k => k.Length).ToList();
            var pattern = @"\b(" + string.Join("|", orderedKeys.Select(Regex.Escape)) + @")\b";
            _replaceRegex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);

            SubscribeLocalEvent<SpanishAccentComponent, AccentGetEvent>(OnAccent);
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
