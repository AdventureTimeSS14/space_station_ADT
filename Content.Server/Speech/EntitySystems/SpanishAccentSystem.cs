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

            message = Regex.Replace(message, "Что", "Que");
            message = Regex.Replace(message, "ЧТО", "QUE");
            message = Regex.Replace(message, "что", "que");

            message = Regex.Replace(message, "Зачем", "Para que");
            message = Regex.Replace(message, "ЗАЧЕМ", "PARA QUE");
            message = Regex.Replace(message, "зачем", "para que");

            message = Regex.Replace(message, "Здравствуйте", "Buenos dias");
            message = Regex.Replace(message, "ЗДРАВСТВУЙТЕ", "BUENOS DIAS");
            message = Regex.Replace(message, "здравствуйте", "buenos dias");

            message = Regex.Replace(message, "Почему", "Por que");
            message = Regex.Replace(message, "ПОЧЕМУ", "POR QUE");
            message = Regex.Replace(message, "почему", "por que");

            message = Regex.Replace(message, "Как", "Como");
            message = Regex.Replace(message, "КАК", "COMO");
            message = Regex.Replace(message, "как", "como");

            message = Regex.Replace(message, "Так", "Asi");
            message = Regex.Replace(message, "ТАК", "ASI");
            message = Regex.Replace(message, "так", "asi");

            message = Regex.Replace(message, "Пожалуйста", "Por favor");
            message = Regex.Replace(message, "ПОЖАЛУЙСТА", "POR FAVOR");
            message = Regex.Replace(message, "пожалуйста", "por favor");

            message = Regex.Replace(message, "Капитан", "Capitan");
            message = Regex.Replace(message, "КАПИТАН", "CAPITAN");
            message = Regex.Replace(message, "капитан", "capitan");

            message = Regex.Replace(message, "Капитана", "Capitan");
            message = Regex.Replace(message, "КАПИТАНА", "CAPITAN");
            message = Regex.Replace(message, "капитана", "capitan");

            message = Regex.Replace(message, "Кеп", "Capitan");
            message = Regex.Replace(message, "КЕП", "CAPITAN");
            message = Regex.Replace(message, "кеп", "capitan");

            message = Regex.Replace(message, "Кепа", "Capitan");
            message = Regex.Replace(message, "КЕПА", "CAPITAN");
            message = Regex.Replace(message, "кепа", "capitan");

            message = Regex.Replace(message, "Друг", "Amigo");
            message = Regex.Replace(message, "ДРУГ", "AMIGO");
            message = Regex.Replace(message, "друг", "amigo");

            message = Regex.Replace(message, "Подруга", "Amiga");
            message = Regex.Replace(message, "ПОДРУГА", "AMIGA");
            message = Regex.Replace(message, "подруга", "amiga");

            message = Regex.Replace(message, "Друзья", "Amigos");
            message = Regex.Replace(message, "ДРУЗЬЯ", "AMIGOS");
            message = Regex.Replace(message, "друзья", "amigos");

            message = Regex.Replace(message, "Подруги", "Amigas");
            message = Regex.Replace(message, "ПОДРУГИ", "AMIGAS");
            message = Regex.Replace(message, "подруги", "amigas");

            message = Regex.Replace(message, "Хорошо", "Bien");
            message = Regex.Replace(message, "ХОРОШО", "BIEN");
            message = Regex.Replace(message, "хорошо", "bien");
            message = Regex.Replace(message, "Хороши", "Bien");
            message = Regex.Replace(message, "ХОРОШИ", "BIEN");
            message = Regex.Replace(message, "хороши", "bien");

            message = Regex.Replace(message, "Мой", "Mi");
            message = Regex.Replace(message, "МОЙ", "MI");
            message = Regex.Replace(message, "мой", "mi");
            message = Regex.Replace(message, "Мое", "Mi");
            message = Regex.Replace(message, "МОЕ", "MI");
            message = Regex.Replace(message, "мое", "mi");
            message = Regex.Replace(message, "Моё", "Mi");
            message = Regex.Replace(message, "МОЁ", "MI");
            message = Regex.Replace(message, "моё", "mi");

            message = Regex.Replace(message, "Мои", "Mis");
            message = Regex.Replace(message, "МОИ", "MIS");
            message = Regex.Replace(message, "мои", "mis");

            message = Regex.Replace(message, "Да", "Si");
            message = Regex.Replace(message, "ДА", "SI");
            message = Regex.Replace(message, "да", "si");

            message = Regex.Replace(message, "Нет", "No");
            message = Regex.Replace(message, "НЕТ", "NO");
            message = Regex.Replace(message, "нет", "no");

            message = Regex.Replace(message, "Отлично", "Excelente");
            message = Regex.Replace(message, "ОТЛИЧНО", "EXCELENTE");
            message = Regex.Replace(message, "отлично", "excelente");

            message = Regex.Replace(message, "Восхитительно", "Maravilloso");
            message = Regex.Replace(message, "ВОСХИТИТЕЛЬНО", "MARAVILLOSO");
            message = Regex.Replace(message, "восхитительно", "maravilloso");
            message = Regex.Replace(message, "Восхитительна", "Deliciosa");
            message = Regex.Replace(message, "ВОСХИТИТЕЛЬНА", "DELICIOSA");
            message = Regex.Replace(message, "восхитительна", "deliciosa");

            message = Regex.Replace(message, "Прекрасно", "Hermoso");
            message = Regex.Replace(message, "ПРЕКРАСНО", "HERMOSO");
            message = Regex.Replace(message, "прекрасно", "hermoso");
            message = Regex.Replace(message, "Прекрасна", "Hermosa");
            message = Regex.Replace(message, "ПРЕКРАСНА", "HERMOSA");
            message = Regex.Replace(message, "прекрасна", "hermosa");

            message = Regex.Replace(message, "Ассистент", "Asistente");
            message = Regex.Replace(message, "АССИСТЕНТ", "ASISTENTE");
            message = Regex.Replace(message, "ассистент", "asistente");
            message = Regex.Replace(message, "Ассистуха", "Asistente");
            message = Regex.Replace(message, "АССИСТУХА", "ASISTENTE");
            message = Regex.Replace(message, "ассистуха", "asistente");

            message = Regex.Replace(message, "Свинья", "Cerdo");
            message = Regex.Replace(message, "СВИНЬЯ", "CERDO");
            message = Regex.Replace(message, "свинья", "cerdo");

            message = Regex.Replace(message, "Ты", "Tu");
            message = Regex.Replace(message, "ТЫ", "TU");
            message = Regex.Replace(message, "ты", "tu");

            message = Regex.Replace(message, "Спасибо", "Gracias");
            message = Regex.Replace(message, "СПАСИБО", "GRACIAS");
            message = Regex.Replace(message, "спасибо", "gracias");

            message = Regex.Replace(message, "Женщина", "Mujer");
            message = Regex.Replace(message, "ЖЕНЩИНА", "MUJER");
            message = Regex.Replace(message, "женщина", "mujer");

            message = Regex.Replace(message, "Эй", "Oye");
            message = Regex.Replace(message, "ЭЙ", "OYE");
            message = Regex.Replace(message, "эй", "oye");

            message = Regex.Replace(message, "Человек", "Persona");
            message = Regex.Replace(message, "ЧЕЛОВЕК", "PERSONA");
            message = Regex.Replace(message, "человек", "persona");

            message = Regex.Replace(message, "Стоять", "Parar");
            message = Regex.Replace(message, "СТОЯТЬ", "PARAR");
            message = Regex.Replace(message, "стоять", "parar");

            message = Regex.Replace(message, "Привет", "Hola");
            message = Regex.Replace(message, "ПРИВЕТ", "HOLA");
            message = Regex.Replace(message, "привет", "hola");

            message = Regex.Replace(message, "доброе утро", "Buenos dias");
            message = Regex.Replace(message, "ДОБРОЕ УТРО", "BUENOS DIAS");
            message = Regex.Replace(message, "доброе утро", "buenos dias");

            message = Regex.Replace(message, "доброй ночи", "Buenas noches");
            message = Regex.Replace(message, "ДОБРОЙ НОЧИ", "BUENAS NOCHES");
            message = Regex.Replace(message, "доброй ночи", "buenas noches");

            message = Regex.Replace(message, "Пока", "Adios");
            message = Regex.Replace(message, "ПОКА", "ADIOS");
            message = Regex.Replace(message, "пока", "adios");

            message = Regex.Replace(message, "прощай", "Adios");
            message = Regex.Replace(message, "ПРОЩАЙ", "ADIOS");
            message = Regex.Replace(message, "прощай", "adios");

            message = Regex.Replace(message, "до свидания", "Hasta la vista");
            message = Regex.Replace(message, "ДО СВИДАНИЯ", "HASTA LA VISTA");
            message = Regex.Replace(message, "до свидания", "hasta la vista");

            message = Regex.Replace(message, "Сб", "Policia");
            message = Regex.Replace(message, "СБ", "POLICIA");
            message = Regex.Replace(message, "сб", "policia");

            message = Regex.Replace(message, "Си", "Jefe ingeniero");
            message = Regex.Replace(message, "СИ", "JEFE INGENIERO");
            message = Regex.Replace(message, "си", "Jefe ingeniero");

            message = Regex.Replace(message, "ГВ", "Jefe medico");
            message = Regex.Replace(message, "Гв", "Jefe medico");
            message = Regex.Replace(message, "гв", "jefe medico");

            message = Regex.Replace(message, "НР", "Mentor");
            message = Regex.Replace(message, "Нр", "Mentor");
            message = Regex.Replace(message, "нр", "mentor");

            message = Regex.Replace(message, "Мы", "Nosotros");
            message = Regex.Replace(message, "МЫ", "NOSOTROS");
            message = Regex.Replace(message, "мы", "nosotros");

            message = Regex.Replace(message, "Кадет", "Cadete");
            message = Regex.Replace(message, "КАДЕТ", "CADETE");
            message = Regex.Replace(message, "кадет", "cadete");

            message = Regex.Replace(message, "Кадеты", "Cadetes");
            message = Regex.Replace(message, "КАДЕТЫ", "CADETES");
            message = Regex.Replace(message, "кадеты", "cadetes");

            message = Regex.Replace(message, "Офицер", "Oficial");
            message = Regex.Replace(message, "ОФИЦЕР", "OFICIAL");
            message = Regex.Replace(message, "офицер", "oficial");

            message = Regex.Replace(message, "Клоун", "Payaso");
            message = Regex.Replace(message, "КЛОУН", "PAYASO");
            message = Regex.Replace(message, "клоун", "payaso");
            message = Regex.Replace(message, "Клоуны", "Payasos");
            message = Regex.Replace(message, "КЛОУНЫ", "PAYASOS");
            message = Regex.Replace(message, "клоуны", "payasos");
            message = Regex.Replace(message, "Клоуна", "Payaso");
            message = Regex.Replace(message, "КЛОУНА", "PAYASO");
            message = Regex.Replace(message, "клоуна", "payaso");

            message = Regex.Replace(message, "Вульпа", "Zorra");
            message = Regex.Replace(message, "ВУЛЬПА", "ZORRA");
            message = Regex.Replace(message, "вульпа", "zorra");
            message = Regex.Replace(message, "Вульпы", "Zorros");
            message = Regex.Replace(message, "ВУЛЬПЫ", "ZORROS");
            message = Regex.Replace(message, "вульпы", "zorros");
            message = Regex.Replace(message, "Вульп", "Zorro");
            message = Regex.Replace(message, "ВУЛЬП", "ZORRO");
            message = Regex.Replace(message, "вульп", "zorro");

            message = Regex.Replace(message, "Истребить", "Exterminar");
            message = Regex.Replace(message, "ИСТРЕБИТЬ", "EXTERMINAR");
            message = Regex.Replace(message, "истребить", "exterminar");

            message = Regex.Replace(message, "Сжечь", "Quemar");
            message = Regex.Replace(message, "СЖЕЧЬ", "QUEMAR");
            message = Regex.Replace(message, "сжечь", "quemar");

            message = Regex.Replace(message, "Убить", "Matar");
            message = Regex.Replace(message, "УБИТЬ", "MATAR");
            message = Regex.Replace(message, "убить", "matar");
            message = Regex.Replace(message, "Убили", "Matar");
            message = Regex.Replace(message, "УБИЛИ", "MATAR");
            message = Regex.Replace(message, "убили", "matar");
            message = Regex.Replace(message, "Убейте", "Matar");
            message = Regex.Replace(message, "УБЕЙТЕ", "MATAR");
            message = Regex.Replace(message, "убейте", "matar");

            message = Regex.Replace(message, "Пиво", "Cerveza");
            message = Regex.Replace(message, "ПИВО", "CERVEZA");
            message = Regex.Replace(message, "пиво", "cerveza");
            message = Regex.Replace(message, "Пива", "Cerveza");
            message = Regex.Replace(message, "ПИВА", "CERVEZA");
            message = Regex.Replace(message, "пива", "cerveza");

            message = Regex.Replace(message, "Вода", "Agua");
            message = Regex.Replace(message, "ВОДА", "AGUA");
            message = Regex.Replace(message, "вода", "agua");
            message = Regex.Replace(message, "Воды", "Agua");
            message = Regex.Replace(message, "ВОДЫ", "AGUA");
            message = Regex.Replace(message, "воды", "agua");

            message = Regex.Replace(message, "ГП", "Jefe de personal");
            message = Regex.Replace(message, "Гп", "Jefe de personal");
            message = Regex.Replace(message, "гп", "jefe de personal");

            message = Regex.Replace(message, "ГСБ", "Jefe de seguridad");
            message = Regex.Replace(message, "Глава Службы Безопасности", "Jefe de seguridad");
            message = Regex.Replace(message, "гсб", "jefe de seguridad");

            message = Regex.Replace(message, "КМ", "Intendente");
            message = Regex.Replace(message, "Км", "Intendente");
            message = Regex.Replace(message, "Квартирмейстер", "Intendente");
            message = Regex.Replace(message, "км", "intendente");

            message = Regex.Replace(message, "ЯО", "TERRORISTAS");
            message = Regex.Replace(message, "Яо", "Terroristas");
            message = Regex.Replace(message, "Ядерные оперативники", "Terroristas");
            message = Regex.Replace(message, "яо", "terroristas");

            message = Regex.Replace(message, "Похуй", "Me importa un carajo");
            message = Regex.Replace(message, "ПОХУЙ", "ME IMPORTA UN CARAJO");
            message = Regex.Replace(message, "похуй", "me importa un carajo");
            message = Regex.Replace(message, "Похую", "Me importa un carajo");
            message = Regex.Replace(message, "ПОХУЮ", "ME IMPORTA UN CARAJO");
            message = Regex.Replace(message, "похую", "me importa un carajo");

            message = Regex.Replace(message, "Пошел нахуй", "Vete a la mierda");
            message = Regex.Replace(message, "ПОШЕЛ НАХУЙ", "VETE A LA MIERDA");
            message = Regex.Replace(message, "пошел нахуй", "vete a la mierda");
            message = Regex.Replace(message, "Пошли нахуй", "Vete a la mierda");
            message = Regex.Replace(message, "ПОШЛИ НАХУЙ", "VETE A LA MIERDA");
            message = Regex.Replace(message, "пошли нахуй", "vete a la mierda");
            message = Regex.Replace(message, "Пошёл нахуй", "Vete a la mierda");
            message = Regex.Replace(message, "ПОШЁЛ НАХУЙ", "VETE A LA MIERDA");
            message = Regex.Replace(message, "пошёл нахуй", "vete a la mierda");
            message = Regex.Replace(message, "Иди нахуй", "Vete a la mierda");
            message = Regex.Replace(message, "ИДИ НАХУЙ", "VETE A LA MIERDA");
            message = Regex.Replace(message, "иди нахуй", "vete a la mierda");
            message = Regex.Replace(message, "Идите нахуй", "Vete a la mierda");
            message = Regex.Replace(message, "ИДИТЕ НАХУЙ", "VETE A LA MIERDA");
            message = Regex.Replace(message, "идите нахуй", "vete a la mierda");
            message = Regex.Replace(message, "Пошел ты нахуй", "Vete a la mierda");
            message = Regex.Replace(message, "ПОШЕЛ ТЫ НАХУЙ", "VETE A LA MIERDA");
            message = Regex.Replace(message, "пошел ты нахуй", "vete a la mierda");
            message = Regex.Replace(message, "Пошли вы нахуй", "Vete a la mierda");
            message = Regex.Replace(message, "ПОШЛИ ВЫ НАХУЙ", "VETE A LA MIERDA");
            message = Regex.Replace(message, "пошли вы нахуй", "vete a la mierda");
            message = Regex.Replace(message, "Пошёл ты нахуй", "Vete a la mierda");
            message = Regex.Replace(message, "ПОШЁЛ ТЫ НАХУЙ", "VETE A LA MIERDA");
            message = Regex.Replace(message, "пошёл ты нахуй", "vete a la mierda");
            message = Regex.Replace(message, "Иди ты нахуй", "Vete a la mierda");
            message = Regex.Replace(message, "ИДИ ТЫ НАХУЙ", "VETE A LA MIERDA");
            message = Regex.Replace(message, "иди ты нахуй", "vete a la mierda");
            message = Regex.Replace(message, "Идите вы нахуй", "Vete a la mierda");
            message = Regex.Replace(message, "ИДИТЕ ВЫ НАХУЙ", "VETE A LA MIERDA");
            message = Regex.Replace(message, "идите вы нахуй", "vete a la mierda");

            message = Regex.Replace(message, "Блять", "Mierda");
            message = Regex.Replace(message, "БЛЯТЬ", "MIERDA");
            message = Regex.Replace(message, "блять", "mierda");
            message = Regex.Replace(message, "Бля", "Mierda");
            message = Regex.Replace(message, "БЛЯ", "MIERDA");
            message = Regex.Replace(message, "бля", "mierda");

            message = Regex.Replace(message, "Сука", "Perra");
            message = Regex.Replace(message, "СУКА", "PERRA");
            message = Regex.Replace(message, "сука", "perra");

            message = Regex.Replace(message, "Идиот", "Idiota");
            message = Regex.Replace(message, "ИДИОТ", "IDIOTA");
            message = Regex.Replace(message, "идиот", "idiota");
            message = Regex.Replace(message, "Идиоты", "Idiota");
            message = Regex.Replace(message, "ИДИОТЫ", "IDIOTA");
            message = Regex.Replace(message, "идиоты", "idiota");

            message = Regex.Replace(message, "Пидор", "Cabron");
            message = Regex.Replace(message, "ПИДОР", "CABRON");
            message = Regex.Replace(message, "пидор", "cabron");
            message = Regex.Replace(message, "Пидоры", "Maricones");
            message = Regex.Replace(message, "ПИДОРЫ", "MARICONES");
            message = Regex.Replace(message, "пидоры", "maricones");
            message = Regex.Replace(message, "Пидорас", "Maricon");
            message = Regex.Replace(message, "ПИДОРАС", "MARICON");
            message = Regex.Replace(message, "пидорас", "maricon");
            message = Regex.Replace(message, "Пидорасы", "Maricones");
            message = Regex.Replace(message, "ПИДОРАСЫ", "MARICONES");
            message = Regex.Replace(message, "пидорасы", "maricones");

            message = Regex.Replace(message, "Мразь", "Canalla");
            message = Regex.Replace(message, "МРАЗЬ", "CANALLA");
            message = Regex.Replace(message, "мразь", "canalla");
            message = Regex.Replace(message, "Мрази", "Canallas");
            message = Regex.Replace(message, "МРАЗИ", "CANALLAS");
            message = Regex.Replace(message, "мрази", "canallas");

            message = Regex.Replace(message, "Еблан", "Gilipollas");
            message = Regex.Replace(message, "ЕБЛАН", "GILIPOLLAS");
            message = Regex.Replace(message, "еблан", "gilipollas");
            message = Regex.Replace(message, "Ебланы", "Gilipollas");
            message = Regex.Replace(message, "ЕБЛАНЫ", "GILIPOLLAS");
            message = Regex.Replace(message, "ебланы", "gilipollas");
            message = Regex.Replace(message, "Ебланище", "Cabron");
            message = Regex.Replace(message, "ЕБЛАНИЩЕ", "CABRON");
            message = Regex.Replace(message, "ебланище", "cabron");

            message = Regex.Replace(message, "Уебок", "Imbecil");
            message = Regex.Replace(message, "УЕБОК", "IMBECIL");
            message = Regex.Replace(message, "уебок", "imbecil");
            message = Regex.Replace(message, "Уёбок", "Imbecil");
            message = Regex.Replace(message, "УЁБОК", "IMBECIL");
            message = Regex.Replace(message, "уёбок", "imbecil");
            message = Regex.Replace(message, "Уебки", "Imbeciles");
            message = Regex.Replace(message, "УЕБКИ", "IMBECILES");
            message = Regex.Replace(message, "уебки", "imbeciles");
            message = Regex.Replace(message, "Уёбки", "Imbeciles");
            message = Regex.Replace(message, "УЁБКИ", "IMBECILES");
            message = Regex.Replace(message, "уёбки", "imbeciles");

            message = Regex.Replace(message, "Нахуя", "Mierda");
            message = Regex.Replace(message, "НАХУЯ", "MIERDA");
            message = Regex.Replace(message, "нахуя", "mierda");

            args.Message = message;
            //ADT-Tweak-End
        }
    }
}
