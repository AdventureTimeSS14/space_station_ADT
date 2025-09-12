using System.Text;
using Content.Server.Speech.Components;
using System.Text.RegularExpressions; //ADT-Tweak

namespace Content.Server.Speech.EntitySystems
{
    public sealed class SpanishAccentSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize(); //ADT-Tweak
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
                } else
                {
                    msg.Append(s.Insert(s.Length - s.TrimStart().Length, toInsert.ToString()));
                }
            }
            return msg.ToString();
        }

        private void OnAccent(EntityUid uid, SpanishAccentComponent component, AccentGetEvent args)
        {
            //ADT-Tweak-Start
            //args.Message = Accentuate(args.Message);
            var message = args.Message;

            // @"\b\b" вместо обычного "" нужно, чтобы не было ошибок при замене.

            message = Regex.Replace(message, @"\bЗачем\b", "Para que");
            message = Regex.Replace(message, @"\bЗАЧЕМ\b", "PARA QUE");
            message = Regex.Replace(message, @"\bзачем\b", "para que");

            message = Regex.Replace(message, @"\bЗдравствуйте\b", "Hola");
            message = Regex.Replace(message, @"\bЗДРАВСТВУЙТЕ\b", "HOLA");
            message = Regex.Replace(message, @"\bздравствуйте\b", "hola");

            message = Regex.Replace(message, @"\bПочему\b", "Por que");
            message = Regex.Replace(message, @"\bПОЧЕМУ\b", "POR QUE");
            message = Regex.Replace(message, @"\bпочему\b", "por que");

            message = Regex.Replace(message, @"\bКак\b", "Como");
            message = Regex.Replace(message, @"\bКАК\b", "COMO");
            message = Regex.Replace(message, @"\bкак\b", "como");

            message = Regex.Replace(message, @"\bТак\b", "Asi");
            message = Regex.Replace(message, @"\bТАК\b", "ASI");
            message = Regex.Replace(message, @"\bтак\b", "asi");

            message = Regex.Replace(message, @"\bПожалуйста\b", "Por favor");
            message = Regex.Replace(message, @"\bПОЖАЛУЙСТА\b", "POR FAVOR");
            message = Regex.Replace(message, @"\bпожалуйста\b", "por favor");

            message = Regex.Replace(message, @"\bКапитан\b", "Capitan");
            message = Regex.Replace(message, @"\bКАПИТАН\b", "CAPITAN");
            message = Regex.Replace(message, @"\bкапитан\b", "capitan");

            message = Regex.Replace(message, @"\bКапитана\b", "Al capitan");
            message = Regex.Replace(message, @"\bКАПИТАНА\b", "AL CAPITAN");
            message = Regex.Replace(message, @"\bкапитана\b", "al capitan");

            message = Regex.Replace(message, @"\bКеп\b", "Capitan");
            message = Regex.Replace(message, @"\bКЕП\b", "CAPITAN");
            message = Regex.Replace(message, @"\bкеп\b", "capitan");

            message = Regex.Replace(message, @"\bКепа\b", "Al capitan");
            message = Regex.Replace(message, @"\bКЕПА\b", "AL CAPITAN");
            message = Regex.Replace(message, @"\bкепа\b", "al capitan");

            message = Regex.Replace(message, @"\bКэп\b", "Capitan");
            message = Regex.Replace(message, @"\bКЭП\b", "CAPITAN");
            message = Regex.Replace(message, @"\bкэп\b", "capitan");

            message = Regex.Replace(message, @"\bКэпа\b", "Al capitan");
            message = Regex.Replace(message, @"\bКЭПА\b", "AL CAPITAN");
            message = Regex.Replace(message, @"\bкэпа\b", "al capitan");

            message = Regex.Replace(message, @"\bДруг\b", "Amigo");
            message = Regex.Replace(message, @"\bДРУГ\b", "AMIGO");
            message = Regex.Replace(message, @"\bдруг\b", "amigo");

            message = Regex.Replace(message, @"\bПодруга\b", "Amiga");
            message = Regex.Replace(message, @"\bПОДРУГА\b", "AMIGA");
            message = Regex.Replace(message, @"\bподруга\b", "amiga");

            message = Regex.Replace(message, @"\bДрузья\b", "Amigos");
            message = Regex.Replace(message, @"\bДРУЗЬЯ\b", "AMIGOS");
            message = Regex.Replace(message, @"\bдрузья\b", "amigos");

            message = Regex.Replace(message, @"\bПодруги\b", "Amigas");
            message = Regex.Replace(message, @"\bПОДРУГИ\b", "AMIGAS");
            message = Regex.Replace(message, @"\bподруги\b", "amigas");

            message = Regex.Replace(message, @"\bХорошо\b", "Bueno");
            message = Regex.Replace(message, @"\bХОРОШО\b", "BUENO");
            message = Regex.Replace(message, @"\bхорошо\b", "bueno");
            message = Regex.Replace(message, @"\bХороши\b", "Buenos");
            message = Regex.Replace(message, @"\bХОРОШИ\b", "BUENOS");
            message = Regex.Replace(message, @"\bхороши\b", "Buenos");

            message = Regex.Replace(message, @"\bОтлично\b", "Excelente");
            message = Regex.Replace(message, @"\bОТЛИЧНО\b", "EXCELENTE");
            message = Regex.Replace(message, @"\bотлично\b", "excelente");

            message = Regex.Replace(message, @"\bВеликолепно\b", "Magnificamente");
            message = Regex.Replace(message, @"\bВЕЛИКОЛЕПНО\b", "MAGNIFICAMENTE");
            message = Regex.Replace(message, @"\bвеликолепно\b", "magnificamente");
            message = Regex.Replace(message, @"\bВеликолепный\b", "Magnifico");
            message = Regex.Replace(message, @"\bВЕЛИКОЛЕПНЫЙ\b", "MAGNIFICO");
            message = Regex.Replace(message, @"\bвеликолепный\b", "magnifico");
            message = Regex.Replace(message, @"\bВеликолепна\b", "Magnifica");
            message = Regex.Replace(message, @"\bВЕЛИКОЛЕПНА\b", "MAGNIFICA");
            message = Regex.Replace(message, @"\bвеликолепна\b", "magnifica");
            message = Regex.Replace(message, @"\bВеликолепная\b", "Magnifica");
            message = Regex.Replace(message, @"\bВЕЛИКОЛЕПНАЯ\b", "MAGNIFICA");
            message = Regex.Replace(message, @"\bвеликолепная\b", "magnifica");

            message = Regex.Replace(message, @"\bЗамечательно\b", "Maravillosamente");
            message = Regex.Replace(message, @"\bЗАМЕЧАТЕЛЬНО\b", "maravillosamente");
            message = Regex.Replace(message, @"\bзамечательно\b", "maravillosamente");
            message = Regex.Replace(message, @"\bЗамечательный\b", "Admirable");
            message = Regex.Replace(message, @"\bЗАМЕЧАТЕЛЬНЫЙ\b", "ADMIRABLE");
            message = Regex.Replace(message, @"\bзамечательный\b", "admirable");
            message = Regex.Replace(message, @"\bЗамечательна\b", "Admirable");
            message = Regex.Replace(message, @"\bЗАМЕЧАТЕЛЬНА\b", "ADMIRABLE");
            message = Regex.Replace(message, @"\bзамечательна\b", "admirable");
            message = Regex.Replace(message, @"\bЗамечательная\b", "Admirable");
            message = Regex.Replace(message, @"\bЗАМЕЧАТЕЛЬНАЯ\b", "ADMIRABLE");
            message = Regex.Replace(message, @"\bзамечательная\b", "admirable");

            message = Regex.Replace(message, @"\bВосхитительно\b", "Maravilloso");
            message = Regex.Replace(message, @"\bВОСХИТИТЕЛЬНО\b", "MARAVILLOSO");
            message = Regex.Replace(message, @"\bвосхитительно\b", "maravilloso");
            message = Regex.Replace(message, @"\bВосхитительный\b", "Maravilloso");
            message = Regex.Replace(message, @"\bВОСХИТИТЕЛЬНЫЙ\b", "MARAVILLOSO");
            message = Regex.Replace(message, @"\bвосхитительный\b", "maravilloso");
            message = Regex.Replace(message, @"\bВосхитительна\b", "Maravillosa");
            message = Regex.Replace(message, @"\bВОСХИТИТЕЛЬНА\b", "MARAVILLOSA");
            message = Regex.Replace(message, @"\bвосхитительна\b", "maravillosa");
            message = Regex.Replace(message, @"\bВосхитительная\b", "Maravillosa");
            message = Regex.Replace(message, @"\bВОСХИТИТЕЛЬНАЯ\b", "MARAVILLOSA");
            message = Regex.Replace(message, @"\bвосхитительная\b", "maravillosa");

            message = Regex.Replace(message, @"\bПрекрасно\b", "Hermoso");
            message = Regex.Replace(message, @"\bПРЕКРАСНО\b", "HERMOSO");
            message = Regex.Replace(message, @"\bпрекрасно\b", "hermoso");
            message = Regex.Replace(message, @"\bПрекрасный\b", "Hermoso");
            message = Regex.Replace(message, @"\bПРЕКРАСНЫЙ\b", "HERMOSO");
            message = Regex.Replace(message, @"\bпрекрасный\b", "hermoso");
            message = Regex.Replace(message, @"\bПрекрасна\b", "Hermosa");
            message = Regex.Replace(message, @"\bПРЕКРАСНА\b", "HERMOSA");
            message = Regex.Replace(message, @"\bпрекрасна\b", "hermosa");
            message = Regex.Replace(message, @"\bПрекрасная\b", "Hermosa");
            message = Regex.Replace(message, @"\bПРЕКРАСНАЯ\b", "HERMOSA");
            message = Regex.Replace(message, @"\bпрекрасная\b", "hermosa");

            message = Regex.Replace(message, @"\bАссистент\b", "Asistente");
            message = Regex.Replace(message, @"\bАССИСТЕНТ\b", "ASISTENTE");
            message = Regex.Replace(message, @"\bассистент\b", "asistente");
            message = Regex.Replace(message, @"\bАссистуха\b", "Asistente");
            message = Regex.Replace(message, @"\bАССИСТУХА\b", "ASISTENTE");
            message = Regex.Replace(message, @"\bассистуха\b", "asistente");

            message = Regex.Replace(message, @"\bСвинья\b", "Cerdo");
            message = Regex.Replace(message, @"\bСВИНЬЯ\b", "CERDO");
            message = Regex.Replace(message, @"\bсвинья\b", "cerdo");

            message = Regex.Replace(message, @"\bСпасибо\b", "Gracias");
            message = Regex.Replace(message, @"\bСПАСИБО\b", "GRACIAS");
            message = Regex.Replace(message, @"\bспасибо\b", "gracias");

            message = Regex.Replace(message, @"\bЖенщина\b", "Mujer");
            message = Regex.Replace(message, @"\bЖЕНЩИНА\b", "MUJER");
            message = Regex.Replace(message, @"\bженщина\b", "mujer");

            message = Regex.Replace(message, @"\bЭй\b", "Oye");
            message = Regex.Replace(message, @"\bЭЙ\b", "OYE");
            message = Regex.Replace(message, @"\bэй\b", "oye");

            message = Regex.Replace(message, @"\bЧеловек\b", "Persona");
            message = Regex.Replace(message, @"\bЧЕЛОВЕК\b", "PERSONA");
            message = Regex.Replace(message, @"\bчеловек\b", "persona");

            message = Regex.Replace(message, @"\bПривет\b", "Hola");
            message = Regex.Replace(message, @"\bПРИВЕТ\b", "HOLA");
            message = Regex.Replace(message, @"\bпривет\b", "hola");

            message = Regex.Replace(message, @"\bдоброе утро\b", "Buenos dias");
            message = Regex.Replace(message, @"\bДОБРОЕ УТРО\b", "BUENOS DIAS");
            message = Regex.Replace(message, @"\bдоброе утро\b", "buenos dias");

            message = Regex.Replace(message, @"\bдоброй ночи\b", "Buenas noches");
            message = Regex.Replace(message, @"\bДОБРОЙ НОЧИ\b", "BUENAS NOCHES");
            message = Regex.Replace(message, @"\bдоброй ночи\b", "buenas noches");

            message = Regex.Replace(message, @"\bПока\b", "Adios");
            message = Regex.Replace(message, @"\bПОКА\b", "ADIOS");
            message = Regex.Replace(message, @"\bпока\b", "adios");

            message = Regex.Replace(message, @"\bпрощай\b", "Adios");
            message = Regex.Replace(message, @"\bПРОЩАЙ\b", "ADIOS");
            message = Regex.Replace(message, @"\bпрощай\b", "adios");

            message = Regex.Replace(message, @"\bдо свидания\b", "Hasta la vista");
            message = Regex.Replace(message, @"\bДО СВИДАНИЯ\b", "HASTA LA VISTA");
            message = Regex.Replace(message, @"\bдо свидания\b", "hasta la vista");

            message = Regex.Replace(message, @"\bСб\b", "Policia");
            message = Regex.Replace(message, @"\bСБ\b", "Policia");
            message = Regex.Replace(message, @"\bсб\b", "policia");

            message = Regex.Replace(message, @"\bСи\b", "Jefe ingeniero");
            message = Regex.Replace(message, @"\bСИ\b", "Jefe ingeniero");
            message = Regex.Replace(message, @"\bси\b", "jefe ingeniero");

            message = Regex.Replace(message, @"\bГв\b", "Jefe medico");
            message = Regex.Replace(message, @"\bГВ\b", "Jefe medico");
            message = Regex.Replace(message, @"\bгв\b", "jefe medico");

            message = Regex.Replace(message, @"\bНр\b", "Director cientifico");
            message = Regex.Replace(message, @"\bНР\b", "Director cientifico");
            message = Regex.Replace(message, @"\bнр\b", "director cientifico");

            message = Regex.Replace(message, @"\bКадет\b", "Cadete");
            message = Regex.Replace(message, @"\bКАДЕТ\b", "CADETE");
            message = Regex.Replace(message, @"\bкадет\b", "cadete");
            message = Regex.Replace(message, @"\bКадеты\b", "Cadetes");
            message = Regex.Replace(message, @"\bКАДЕТЫ\b", "CADETES");
            message = Regex.Replace(message, @"\bкадеты\b", "cadetes");
            message = Regex.Replace(message, @"\bКадета\b", "Cadete");
            message = Regex.Replace(message, @"\bКАДЕТА\b", "CADETE");
            message = Regex.Replace(message, @"\bкадета\b", "cadete");

            message = Regex.Replace(message, @"\bОфицер\b", "Oficial");
            message = Regex.Replace(message, @"\bОФИЦЕР\b", "OFICIAL");
            message = Regex.Replace(message, @"\bофицер\b", "oficial");
            message = Regex.Replace(message, @"\bОфицеры\b", "Oficialilad");
            message = Regex.Replace(message, @"\bОФИЦЕРЫ\b", "OFICIALILAD");
            message = Regex.Replace(message, @"\bофицеры\b", "oficialidad");
            message = Regex.Replace(message, @"\bОфицера\b", "Oficial");
            message = Regex.Replace(message, @"\bОФИЦЕРА\b", "OFICIAL");
            message = Regex.Replace(message, @"\bофицера\b", "oficial");

            message = Regex.Replace(message, @"\bКлоун\b", "Payaso");
            message = Regex.Replace(message, @"\bКЛОУН\b", "PAYASO");
            message = Regex.Replace(message, @"\bклоун\b", "payaso");
            message = Regex.Replace(message, @"\bКлоуны\b", "Payasos");
            message = Regex.Replace(message, @"\bКЛОУНЫ\b", "PAYASOS");
            message = Regex.Replace(message, @"\bклоуны\b", "payasos");
            message = Regex.Replace(message, @"\bКлоуна\b", "Payaso");
            message = Regex.Replace(message, @"\bКЛОУНА\b", "PAYASO");
            message = Regex.Replace(message, @"\bклоуна\b", "payaso");

            message = Regex.Replace(message, @"\bВульпы\b", "Zorros");
            message = Regex.Replace(message, @"\bВУЛЬПЫ\b", "ZORROS");
            message = Regex.Replace(message, @"\bвульпы\b", "zorros");
            message = Regex.Replace(message, @"\bВульп\b", "Zorro");
            message = Regex.Replace(message, @"\bВУЛЬП\b", "ZORRO");
            message = Regex.Replace(message, @"\bвульп\b", "zorro");
            message = Regex.Replace(message, @"\bВульпа\b", "Zorra");
            message = Regex.Replace(message, @"\bВУЛЬПА\b", "ZORRA");
            message = Regex.Replace(message, @"\bвульпа\b", "zorra");

            message = Regex.Replace(message, @"\bИстребить\b", "Exterminar");
            message = Regex.Replace(message, @"\bИСТРЕБИТЬ\b", "EXTERMINAR");
            message = Regex.Replace(message, @"\bистребить\b", "exterminar");

            message = Regex.Replace(message, @"\bПиво\b", "Cerveza");
            message = Regex.Replace(message, @"\bПИВО\b", "CERVEZA");
            message = Regex.Replace(message, @"\bпиво\b", "cerveza");
            message = Regex.Replace(message, @"\bПива\b", "Cerveza");
            message = Regex.Replace(message, @"\bПИВА\b", "CERVEZA");
            message = Regex.Replace(message, @"\bпива\b", "cerveza");

            message = Regex.Replace(message, @"\bВода\b", "Agua");
            message = Regex.Replace(message, @"\bВОДА\b", "AGUA");
            message = Regex.Replace(message, @"\bвода\b", "agua");
            message = Regex.Replace(message, @"\bВоды\b", "Agua");
            message = Regex.Replace(message, @"\bВОДЫ\b", "AGUA");
            message = Regex.Replace(message, @"\bводы\b", "agua");

            message = Regex.Replace(message, @"\bГп\b", "Jefe de personal");
            message = Regex.Replace(message, @"\bГП\b", "Jefe de personal");
            message = Regex.Replace(message, @"\bгп\b", "jefe de personal");

            message = Regex.Replace(message, @"\bГсб\b", "Jefe de seguridad");
            message = Regex.Replace(message, @"\bГСБ\b", "Jefe de seguridad");
            message = Regex.Replace(message, @"\bгсб\b", "jefe de seguridad");

            message = Regex.Replace(message, @"\bКм\b", "Intendente");
            message = Regex.Replace(message, @"\bКМ\b", "Intendente");
            message = Regex.Replace(message, @"\bкм\b", "intendente");

            message = Regex.Replace(message, @"\bЯо\b", "Operativos nucleares");
            message = Regex.Replace(message, @"\bЯО\b", "OPERATIVOS NUCLEARES");
            message = Regex.Replace(message, @"\bяо\b", "operativos nucleares");
            message = Regex.Replace(message, @"\bЯдерные оперативники\b", "Operativos nucleares");
            message = Regex.Replace(message, @"\bЯДЕРНЫЕ ОПЕРАТИВНИКИ\b", "OPERATIVOS NUCLEARES");
            message = Regex.Replace(message, @"\bядерные оперативники\b", "operativos nucleares");

            message = Regex.Replace(message, @"\bТеррорист\b", "Terrorista");
            message = Regex.Replace(message, @"\bТЕРРОРИСТ\b", "TERRORISTA");
            message = Regex.Replace(message, @"\bтеррорист\b", "terrorista");
            message = Regex.Replace(message, @"\bТеррористы\b", "Terroristas");
            message = Regex.Replace(message, @"\bТЕРРОРИСТЫ\b", "TERRORISTAS");
            message = Regex.Replace(message, @"\bтеррористы\b", "terroristas");
            message = Regex.Replace(message, @"\bТеррориста\b", "Terrorista");
            message = Regex.Replace(message, @"\bТЕРРОРИСТА\b", "TERRORISTA");
            message = Regex.Replace(message, @"\bтеррориста\b", "terrorista");

            // Ругательства

            message = Regex.Replace(message, @"\bПохуй\b", "Me importa un carajo");
            message = Regex.Replace(message, @"\bПОХУЙ\b", "ME IMPORTA UN CARAJO");
            message = Regex.Replace(message, @"\bпохуй\b", "me importa un carajo");
            message = Regex.Replace(message, @"\bПохую\b", "Me importa un carajo");
            message = Regex.Replace(message, @"\bПОХУЮ\b", "ME IMPORTA UN CARAJO");
            message = Regex.Replace(message, @"\bпохую\b", "me importa un carajo");

            message = Regex.Replace(message, @"\bПошел нахуй\b", "Vete a la mierda");
            message = Regex.Replace(message, @"\bПОШЕЛ НАХУЙ\b", "VETE A LA MIERDA");
            message = Regex.Replace(message, @"\bпошел нахуй\b", "vete a la mierda");
            message = Regex.Replace(message, @"\bПошли нахуй\b", "Vete a la mierda");
            message = Regex.Replace(message, @"\bПОШЛИ НАХУЙ\b", "VETE A LA MIERDA");
            message = Regex.Replace(message, @"\bпошли нахуй\b", "vete a la mierda");
            message = Regex.Replace(message, @"\bПошёл нахуй\b", "Vete a la mierda");
            message = Regex.Replace(message, @"\bПОШЁЛ НАХУЙ\b", "VETE A LA MIERDA");
            message = Regex.Replace(message, @"\bпошёл нахуй\b", "vete a la mierda");
            message = Regex.Replace(message, @"\bИди нахуй\b", "Vete a la mierda");
            message = Regex.Replace(message, @"\bИДИ НАХУЙ\b", "VETE A LA MIERDA");
            message = Regex.Replace(message, @"\bиди нахуй\b", "vete a la mierda");
            message = Regex.Replace(message, @"\bИдите нахуй\b", "Vete a la mierda");
            message = Regex.Replace(message, @"\bИДИТЕ НАХУЙ\b", "VETE A LA MIERDA");
            message = Regex.Replace(message, @"\bидите нахуй\b", "vete a la mierda");
            message = Regex.Replace(message, @"\bПошел ты нахуй\b", "Vete a la mierda");
            message = Regex.Replace(message, @"\bПОШЕЛ ТЫ НАХУЙ\b", "VETE A LA MIERDA");
            message = Regex.Replace(message, @"\bпошел ты нахуй\b", "vete a la mierda");
            message = Regex.Replace(message, @"\bПошли вы нахуй\b", "Vete a la mierda");
            message = Regex.Replace(message, @"\bПОШЛИ ВЫ НАХУЙ\b", "VETE A LA MIERDA");
            message = Regex.Replace(message, @"\bпошли вы нахуй\b", "vete a la mierda");
            message = Regex.Replace(message, @"\bПошёл ты нахуй\b", "Vete a la mierda");
            message = Regex.Replace(message, @"\bПОШЁЛ ТЫ НАХУЙ\b", "VETE A LA MIERDA");
            message = Regex.Replace(message, @"\bпошёл ты нахуй\b", "vete a la mierda");
            message = Regex.Replace(message, @"\bИди ты нахуй\b", "Vete a la mierda");
            message = Regex.Replace(message, @"\bИДИ ТЫ НАХУЙ\b", "VETE A LA MIERDA");
            message = Regex.Replace(message, @"\bиди ты нахуй\b", "vete a la mierda");
            message = Regex.Replace(message, @"\bИдите вы нахуй\b", "Vete a la mierda");
            message = Regex.Replace(message, @"\bИДИТЕ ВЫ НАХУЙ\b", "VETE A LA MIERDA");
            message = Regex.Replace(message, @"\bидите вы нахуй\b", "vete a la mierda");

            message = Regex.Replace(message, @"\bБлять\b", "Mierda");
            message = Regex.Replace(message, @"\bБЛЯТЬ\b", "MIERDA");
            message = Regex.Replace(message, @"\bблять\b", "mierda");
            message = Regex.Replace(message, @"\bБля\b", "Mierda");
            message = Regex.Replace(message, @"\bБЛЯ\b", "MIERDA");
            message = Regex.Replace(message, @"\bбля\b", "mierda");

            message = Regex.Replace(message, @"\bСука\b", "Perra");
            message = Regex.Replace(message, @"\bСУКА\b", "PERRA");
            message = Regex.Replace(message, @"\bсука\b", "perra");
            message = Regex.Replace(message, @"\bСуку\b", "Perro");
            message = Regex.Replace(message, @"\bСУКУ\b", "PERRO");
            message = Regex.Replace(message, @"\bсуку\b", "perro");
            message = Regex.Replace(message, @"\bСуки\b", "Perros");
            message = Regex.Replace(message, @"\bСУКИ\b", "PERROS");
            message = Regex.Replace(message, @"\bсуки\b", "perros");

            message = Regex.Replace(message, @"\bИдиот\b", "Idiota");
            message = Regex.Replace(message, @"\bИДИОТ\b", "IDIOTA");
            message = Regex.Replace(message, @"\bидиот\b", "idiota");
            message = Regex.Replace(message, @"\bИдиоты\b", "Idiota");
            message = Regex.Replace(message, @"\bИДИОТЫ\b", "IDIOTA");
            message = Regex.Replace(message, @"\bидиоты\b", "idiota");
            message = Regex.Replace(message, @"\bИдиотов\b", "Idiotas");
            message = Regex.Replace(message, @"\bИДИОТОВ\b", "IDIOTAS");
            message = Regex.Replace(message, @"\bидиотов\b", "idiotas");

            message = Regex.Replace(message, @"\bПидор\b", "Cabron");
            message = Regex.Replace(message, @"\bПИДОР\b", "CABRON");
            message = Regex.Replace(message, @"\bпидор\b", "cabron");
            message = Regex.Replace(message, @"\bПидоры\b", "Maricones");
            message = Regex.Replace(message, @"\bПИДОРЫ\b", "MARICONES");
            message = Regex.Replace(message, @"\bпидоры\b", "maricones");
            message = Regex.Replace(message, @"\bПидора\b", "Cabron");
            message = Regex.Replace(message, @"\bПИДОРА\b", "CABRON");
            message = Regex.Replace(message, @"\bпидора\b", "cabron");
            message = Regex.Replace(message, @"\bПидорас\b", "Maricon");
            message = Regex.Replace(message, @"\bПИДОРАС\b", "MARICON");
            message = Regex.Replace(message, @"\bпидорас\b", "maricon");
            message = Regex.Replace(message, @"\bПидорасы\b", "Maricones");
            message = Regex.Replace(message, @"\bПИДОРАСЫ\b", "MARICONES");
            message = Regex.Replace(message, @"\bпидорасы\b", "maricones");
            message = Regex.Replace(message, @"\bПидораса\b", "Maricon");
            message = Regex.Replace(message, @"\bПИДОРАСА\b", "MARICON");
            message = Regex.Replace(message, @"\bпидораса\b", "maricon");

            message = Regex.Replace(message, @"\bМразь\b", "Canalla");
            message = Regex.Replace(message, @"\bМРАЗЬ\b", "CANALLA");
            message = Regex.Replace(message, @"\bмразь\b", "canalla");
            message = Regex.Replace(message, @"\bМрази\b", "Canallas");
            message = Regex.Replace(message, @"\bМРАЗИ\b", "CANALLAS");
            message = Regex.Replace(message, @"\bмрази\b", "canallas");

            message = Regex.Replace(message, @"\bЕблан\b", "Gilipollas");
            message = Regex.Replace(message, @"\bЕБЛАН\b", "GILIPOLLAS");
            message = Regex.Replace(message, @"\bеблан\b", "gilipollas");
            message = Regex.Replace(message, @"\bЕбланы\b", "Gilipollas");
            message = Regex.Replace(message, @"\bЕБЛАНЫ\b", "GILIPOLLAS");
            message = Regex.Replace(message, @"\bебланы\b", "gilipollas");
            message = Regex.Replace(message, @"\bЕблана\b", "Gilipollas");
            message = Regex.Replace(message, @"\bЕБЛАНА\b", "GILIPOLLAS");
            message = Regex.Replace(message, @"\bеблана\b", "gilipollas");
            message = Regex.Replace(message, @"\bЕбланище\b", "Cabron");
            message = Regex.Replace(message, @"\bЕБЛАНИЩЕ\b", "CABRON");
            message = Regex.Replace(message, @"\bебланище\b", "cabron");
            message = Regex.Replace(message, @"\bЕбланища\b", "Cabron");
            message = Regex.Replace(message, @"\bЕБЛАНИЩА\b", "CABRON");
            message = Regex.Replace(message, @"\bебланища\b", "cabron");

            message = Regex.Replace(message, @"\bУебок\b", "Hijo de puta");
            message = Regex.Replace(message, @"\bУЕБОК\b", "HIJO DE PUTA");
            message = Regex.Replace(message, @"\bуебок\b", "hijo de puta");
            message = Regex.Replace(message, @"\bУёбок\b", "Hijo de puta");
            message = Regex.Replace(message, @"\bУЁБОК\b", "HIJO DE PUTA");
            message = Regex.Replace(message, @"\bуёбок\b", "hijo de puta");
            message = Regex.Replace(message, @"\bУебка\b", "Hijo de puta");
            message = Regex.Replace(message, @"\bУЕБКА\b", "HIJO DE PUTA");
            message = Regex.Replace(message, @"\bуебка\b", "hijo de puta");
            message = Regex.Replace(message, @"\bУёбка\b", "Hijo de puta");
            message = Regex.Replace(message, @"\bУЁБКА\b", "HIJO DE PUTA");
            message = Regex.Replace(message, @"\bуёбка\b", "hijo de puta");
            message = Regex.Replace(message, @"\bУебки\b", "Hijos de puta");
            message = Regex.Replace(message, @"\bУЕБКИ\b", "HIJOS DE PUTA");
            message = Regex.Replace(message, @"\bуебки\b", "hijos de puta");
            message = Regex.Replace(message, @"\bУёбки\b", "Hijos de puta");
            message = Regex.Replace(message, @"\bУЁБКИ\b", "HIJOS DE PUTA");
            message = Regex.Replace(message, @"\bуёбки\b", "hijos de puta");
            message = Regex.Replace(message, @"\bУебков\b", "Hijos de puta");
            message = Regex.Replace(message, @"\bУЕБКОВ\b", "HIJOS DE PUTA");
            message = Regex.Replace(message, @"\bуебков\b", "hijos de puta");
            message = Regex.Replace(message, @"\bУёбков\b", "Hijos de puta");
            message = Regex.Replace(message, @"\bУЁБКОВ\b", "HIJOS DE PUTA");
            message = Regex.Replace(message, @"\bуёбков\b", "hijos de puta");

            message = Regex.Replace(message, @"\bНахуя\b", "Para que mierda");
            message = Regex.Replace(message, @"\bНАХУЯ\b", "PARA QUE MIERDA");
            message = Regex.Replace(message, @"\bнахуя\b", "para que mierda");
            message = Regex.Replace(message, @"\bХуйня\b", "Mierda");
            message = Regex.Replace(message, @"\bХУЙНЯ\b", "MIERDA");
            message = Regex.Replace(message, @"\bхуйня\b", "mierda");
            message = Regex.Replace(message, @"\bНахуй\b", "A la mierda");
            message = Regex.Replace(message, @"\bНАХУЙ\b", "A LA MIERDA");
            message = Regex.Replace(message, @"\bнахуй\b", "a la mierda");

            args.Message = message;
            //ADT-Tweak-End
        }
    }
}
