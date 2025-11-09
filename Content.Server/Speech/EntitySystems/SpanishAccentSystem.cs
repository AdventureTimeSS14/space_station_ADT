using System.Text;
using System.Text.RegularExpressions; //ADT-Tweak
using Content.Server.Speech.Components;
using Content.Shared.Speech;
using System.Linq; //ADT-Tweak

namespace Content.Server.Speech.EntitySystems
{
    public sealed class SpanishAccentSystem : EntitySystem
    {
        //ADT-Tweak-Start
        private readonly Dictionary<string, string> _replacements = new();
        private Regex? _replaceRegex;

        // Инструкции для TTS-а, чтобы слова произносились правильно
        private readonly Dictionary<string, string> _pronunciations = new()
        {
            {"para qué", "пара кэ"},
            {"por qué", "пор кэ"},
            {"cómo", "комо"},
            {"así", "аси"},
            {"me", "мэ"},
            {"por favor", "пор фавор"},
            {"por favor, hazlo", "пор фавор, асло"},
            {"perdón", "пар-дон"},
            {"amigo", "амиго"},
            {"al amigo", "аль амиго"},
            {"amigos", "амиго-с"},
            {"amiguito", "амигито"},
            {"amiga", "амига"},
            {"a la amiga", "а ла амига"},
            {"amigas", "амига-с"},
            {"amiguita", "амигита"},
            {"compañero", "компаньээро"},
            {"al compañero", "аль компаньээро"},
            {"compañeros", "компаньээро-с"},
            {"hermano", "эрмано"},
            {"al hermano", "аль эрмано"},
            {"hermanos", "эрмано-с"},
            {"hermana", "эрмана"},
            {"a la hermana", "а ла эрмана"},
            {"hermanas", "эрмана-с"},
            {"padre", "падрэ"},
            {"al padre", "аль падрэ"},
            {"padres", "падрэ-с"},
            {"bien", "бьен"},
            {"bueno", "буээно"},
            {"buena", "буээна"},
            {"buenos", "буээно-с"},
            {"excelente", "эхсэлентэ"},
            {"magníficamente", "магнифика-мен-тэ"},
            {"magnífico", "магнифико"},
            {"magníficos", "магнифико-с"},
            {"magnífica", "магнифика"},
            {"estupendo", "эступэндо"},
            {"estupendos", "эступэндо-с"},
            {"maravillosamente", "маравий-о-самэн-тэ"},
            {"maravilloso", "маравийосо"},
            {"maravillosos", "маравийосо-с"},
            {"maravillosa", "маравийоса"},
            {"hermosamente", "эрмосамэн-тэ"},
            {"hermoso", "эрмосо"},
            {"hermosos", "эрмосо-с"},
            {"hermosa", "эрмоса"},
            {"asistente", "асистэнтэ"},
            {"ayudante", "айудантэ"},
            {"cerdo", "сэрдо"},
            {"cerdos", "сэрдо-с"},
            {"gracias", "гра-сиа-с"},
            {"muchas gracias", "му-час гра-сиа-с"},
            {"mujer", "мухэр"},
            {"oye", "ойэ"},
            {"persona", "пэрсона"},
            {"hola", "оола"},
            {"buenos días", "буэно-с ди-ас"},
            {"buenas tardes", "буэна-с тардэс"},
            {"buenas noches", "буэна-с ночэс"},
            {"adiós", "адьёс"},
            {"adiós a todos", "адьёс а тодос"},
            {"hasta la vista", "аста ла виста"},
            {"payaso", "па-йасс-о"},
            {"al payaso", "аль па-йасс-о"},
            {"payasos", "па-йасс-о-с"},
            {"payasito", "па-йаси-то"},
            {"al payasito", "аль па-йаси-то"},
            {"payasitos", "па-йаси-то-с"},
            {"vamos", "вамос"},
            {"zorras", "сорра-с"},
            {"zorro", "сорро"},
            {"zorra", "сорра"},
            {"a la zorra", "а ла сорра"},
            {"exterminar", "экстэрминар"},
            {"cerveza", "сэр-вэса"},
            {"agua", "агуа"},
            {"agente", "ахьентэ"},
            {"al agente", "аль ахьентэ"},
            {"agentes", "ахьентэ-с"},
            {"operativos nucleares", "оперативо-с нуклеар-эс"},
            {"operativo", "оперативо"},
            {"al operativo", "аль оперативо"},
            {"operativos", "оперативо-с"},
            {"terrorista", "тэррориста"},
            {"terroristas", "тэррориста-с"},
            {"corporación", "корпорасьон"},
            {"corporaciones", "корпорасьон-эс"},
            {"empresario", "эмпресарйо"},
            {"al empresario", "аль эмпресарйо"},
            {"empresarios", "эмпресарйо-с"},
            {"capitán", "капитан"},
            {"al capitán", "аль капитан"},
            {"jefe ingeniero", "хэфэ ин-хэ-нь-э-ро"},
            {"jefe médico", "хэфэ мэдико"},
            {"director científico", "дирэктор сиэнтифико"},
            {"al director científico", "аль дирэктор сиэнтифико"},
            {"jefe de personal", "хэфэ дэ пэрсон-нал"},
            {"jefe de seguridad", "хэфэ дэ сэгуридад"},
            {"intendente", "интендэнтэ"},
            {"al intendente", "аль интендэнтэ"},
            {"policía", "полисиа"},
            {"de seguridad", "дэ сэгуридад"},
            {"seguras", "сэгура-с"},
            {"segura", "сэгура"},
            {"al segura", "аль сэгура"},
            {"segurata", "сэгурата"},
            {"al segurata", "аль сэгурата"},
            {"seguratas", "сэгурата-с"},
            {"cadete", "кади-этэ"},
            {"cadetes", "кади-этэ-с"},
            {"al cadete", "аль кади-этэ"},
            {"oficial", "офисиал"},
            {"oficiales", "офисиал-эс"},
            {"al oficial", "аль офисиал"},
            {"agente de asuntos internos", "ахьентэ дэ асунтос интерно-с"},
            {"magistrado", "магистрадо"},
            {"al magistrado", "аль магистрадо"},
            {"magistrados", "магистрадо-с"},
            {"oficial del escudo azul", "офисиал дэль эскудо асуль"},
            {"comando central", "командо сэнтрал"},
            {"monstruo", "мон-струо"},
            {"al monstruo", "аль мон-струо"},
            {"monstruos", "мон-струо-с"},
            {"bastardo", "бастардо"},
            {"al bastardo", "аль бастардо"},
            {"bastardos", "бастардо-с"},
            {"puta madre", "пута мадрэ"},
            {"de puta madre", "дэ пута мадрэ"},
            {"vete a la mierda", "вэтэ а ла мьерда"},
            {"váyanse a la mierda", "байансэ а ла мьерда"},
            {"mierda", "мьерда"},
            {"perra", "пэрра"},
            {"perras", "пэрра-с"},
            {"a la perra", "а ла пэрра"},
            {"perrita", "пэррита"},
            {"idiota", "иди-ота"},
            {"idiotas", "иди-ота-с"},
            {"al idiota", "аль иди-ота"},
            {"cabrón", "каброн"},
            {"cabrones", "каброн-эс"},
            {"al cabrón", "аль каброн"},
            {"maricón", "марикон"},
            {"maricones", "марикон-эс"},
            {"al maricón", "аль марикон"},
            {"canalla", "канайя"},
            {"al canalla", "аль канайя"},
            {"canallas", "канайя-с"},
            {"gilipollas", "хилипойя-с"},
            {"al gilipollas", "аль хилипойя-с"},
            {"hijo de puta", "ихо дэ пута"},
            {"hijos de puta", "ихос дэ пута"},
            {"a los hijos de puta", "а лос ихос дэ пута"},
            {"importa un carajo", "им-пор-та ун карахо"},
            {"importa un bledo", "им-пор-та ун блэдо"},
            {"no me importa", "но мэ им-пор-та"},
            {"para qué mierda", "пара кэ мьерда"},
            {"hostia puta", "остиа пута"}
        };

        // Испанский акцент
        private Regex? _pronounceRegex;
        //ADT-Tweak-End

        public override void Initialize()
        {
            //ADT-Tweak-Start
            base.Initialize();

            _replacements.Add("зачем", "para qué");
            _replacements.Add("почему", "por qué");
            _replacements.Add("как", "cómo");
            _replacements.Add("так", "así");
            _replacements.Add("мне", "me");

            _replacements.Add("пожалуйста", "por favor");
            _replacements.Add("ну пожалуйста", "por favor, hazlo");

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

            _replacements.Add("хорошо", "bien");
            _replacements.Add("хорош", "bueno");
            _replacements.Add("хороша", "buena");
            _replacements.Add("хороший", "bueno");
            _replacements.Add("хорошая", "buena");
            _replacements.Add("хороши", "buenos");
            _replacements.Add("хорошие", "buenos");
            _replacements.Add("хороших", "buenos");
            _replacements.Add("хорошего", "buenos");

            _replacements.Add("отлично", "excelente");

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
            _replacements.Add("спасибо большое", "muchas gracias");
            _replacements.Add("большое спасибо", "muchas gracias");

            _replacements.Add("женщина", "mujer");
            _replacements.Add("эй", "oye");
            _replacements.Add("человек", "persona");

            _replacements.Add("привет", "hola");
            _replacements.Add("здравствуйте", "hola");
            _replacements.Add("доброе утро", "buenos días");
            _replacements.Add("добрый вечер", "buenas tardes");
            _replacements.Add("доброй ночи", "buenas noches");

            _replacements.Add("пока ", "пока "); // Чтобы не было ошибок с: "пока что"; "пока я/он/они"
            _replacements.Add("пока", "adiós");
            _replacements.Add("пока пока", "adiós");
            _replacements.Add("прощай", "adiós");
            _replacements.Add("прощайте", "adiós a todos");
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
            _replacements.Add("идем", "vamos");
            _replacements.Add("вперед", "vamos");
            _replacements.Add("идём", "vamos");
            _replacements.Add("вперёд", "vamos");

            _replacements.Add("вульпы", "zorras");
            _replacements.Add("вульп", "zorro");
            _replacements.Add("вульпа", "zorra");
            _replacements.Add("вульпу", "zorra");
            _replacements.Add("вульпе", "a la zorra");

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

            _replacements.Add("корпорация", "corporación");
            _replacements.Add("корпорации", "corporaciones");
            _replacements.Add("корпораций", "corporaciones");
            _replacements.Add("корпорацию", "corporación");

            _replacements.Add("корпорат", "empresario");
            _replacements.Add("корпорату", "al empresario");
            _replacements.Add("корпората", "empresario");
            _replacements.Add("корпораты", "empresarios");
            _replacements.Add("корпоратов", "empresarios");

            // Командование

            _replacements.Add("капитан", "capitán");
            _replacements.Add("капитана", "al capitán");
            _replacements.Add("капитану", "al capitán");
            _replacements.Add("кеп", "capitán");
            _replacements.Add("кепа", "al capitán");
            _replacements.Add("кепу", "al capitán");
            _replacements.Add("кэп", "capitán");
            _replacements.Add("кэпа", "al capitán");
            _replacements.Add("кэпу", "al capitán");

            _replacements.Add("си", "jefe ingeniero");
            _replacements.Add("гв", "jefe médico");
            _replacements.Add("нр", "director científico");
            _replacements.Add("нра", "director científico");
            _replacements.Add("нр-а", "director científico");
            _replacements.Add("нру", "al director científico");
            _replacements.Add("нр-у", "al director científico");
            _replacements.Add("гп", "jefe de personal");
            _replacements.Add("гсб", "jefe de seguridad");
            _replacements.Add("км", "intendente");
            _replacements.Add("кма", "intendente");
            _replacements.Add("км-а", "intendente");
            _replacements.Add("кму", "al intendente");
            _replacements.Add("км-у", "al intendente");

            // Служба Безопасности

            _replacements.Add("сб", "policía");
            _replacements.Add(" сб", " de seguridad"); // Чтобы офицер СБ произносилось как: "oficial de seguridad"
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

            _replacements.Add("пошел нахуй", "vete a la mierda");
            _replacements.Add("пошли нахуй", "váyanse a la mierda");
            _replacements.Add("пошёл нахуй", "vete a la mierda");
            _replacements.Add("иди нахуй", "vete a la mierda");
            _replacements.Add("идите нахуй", "váyanse a la mierda");
            _replacements.Add("пошел ты нахуй", "vete a la mierda");
            _replacements.Add("пошли вы нахуй", "váyanse a la mierda");
            _replacements.Add("пошёл ты нахуй", "vete a la mierda");
            _replacements.Add("иди ты нахуй", "vete a la mierda");
            _replacements.Add("идите вы нахуй", "váyanse a la mierda");
            _replacements.Add("съеби", "vete a la mierda");
            _replacements.Add("съебите", "váyanse a la mierda");
            _replacements.Add("съебись", "vete a la mierda");
            _replacements.Add("съебитесь", "váyanse a la mierda");

            _replacements.Add("блять", "mierda");
            _replacements.Add("бля", "mierda");
            _replacements.Add("бляха", "mierda");

            _replacements.Add("сука", "perra");
            _replacements.Add("суку", "perra");
            _replacements.Add("суки", "perras");
            _replacements.Add("сук", "perras");
            _replacements.Add("суке", "a la perra");
            _replacements.Add("сучка", "perrita");

            _replacements.Add("идиот", "idiota");
            _replacements.Add("идиоты", "idiotas");
            _replacements.Add("идиота", "idiota");
            _replacements.Add("идиоту", "al idiota");
            _replacements.Add("идиотов", "idiotas");

            _replacements.Add("пидор", "cabrón");
            _replacements.Add("пидоры", "cabrones");
            _replacements.Add("пидора", "cabrón");
            _replacements.Add("пидору", "al cabrón");
            _replacements.Add("пидоров", "cabrones");
            _replacements.Add("пидорас", "maricón");
            _replacements.Add("пидорасы", "maricones");
            _replacements.Add("пидораса", "maricón");
            _replacements.Add("пидорасу", "al maricón");
            _replacements.Add("пидорасов", "maricones");

            _replacements.Add("мразь", "canalla");
            _replacements.Add("мразе", "canalla");
            _replacements.Add("мрази", "canallas");

            _replacements.Add("мерзавец", "canalla");
            _replacements.Add("мерзавцев", "canallas");
            _replacements.Add("мерзавцы", "canallas");
            _replacements.Add("мерзавца", "canalla");
            _replacements.Add("мерзавцу", "al canalla");
            _replacements.Add("мерзавцов", "canallas");

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
            _replacements.Add("уебку", "a los hijos de puta");
            _replacements.Add("уёбку", "a los hijos de puta");

            _replacements.Add("похуй", "importa un carajo");
            _replacements.Add("поебать", "importa un carajo");
            _replacements.Add("похую", "importa un carajo");
            _replacements.Add("похер", "importa un bledo");
            _replacements.Add("плевать", "no me importa");
            _replacements.Add("мне плевать", "no me importa");

            _replacements.Add("нахера", "para qué mierda");
            _replacements.Add("нахуя", "para qué mierda");

            _replacements.Add("ахуеть", "hostia puta");
            _replacements.Add("охуеть", "hostia puta");
            _replacements.Add("ахуй", "hostia puta");

            _replacements.Add("хуйня", "mierda");

            var orderedKeys = _replacements.Keys.OrderByDescending(k => k.Length).ToList();
            var pattern = @"\b(" + string.Join("|", orderedKeys.Select(Regex.Escape)) + @")\b";
            _replaceRegex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

            var orderedPronounceKeys = _pronunciations.Keys.OrderByDescending(k => k.Length).ToList();
            var pronouncePattern = @"\b(" + string.Join("|", orderedPronounceKeys.Select(Regex.Escape)) + @")\b";
            _pronounceRegex = new Regex(pronouncePattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
            //ADT-Tweak-End

            SubscribeLocalEvent<SpanishAccentComponent, AccentGetEvent>(OnAccent);
        }

        //ADT-Tweak-Start
        public string GetPronunciation(string message)
        {
            if (string.IsNullOrEmpty(message) || _pronounceRegex == null)
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
        //ADT-Tweak-End

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
                    //ADT-Tweak-Start
                }
                else
                //ADT-Tweak-End
                {
                    msg.Append(s.Insert(s.Length - s.TrimStart().Length, toInsert.ToString()));
                }
            }
            return msg.ToString();
        }

        private void OnAccent(EntityUid uid, SpanishAccentComponent component, AccentGetEvent args)
        {
            //ADT-Tweak-Start
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
            //ADT-Tweak-End
        }
    }
}
