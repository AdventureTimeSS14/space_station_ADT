using System.Text;
using Content.Server.Speech.Components;
using System.Text.RegularExpressions;
//ADT-Tweak-Start
using System.Globalization;
using System.Linq;
//ADT-Tweak-End

namespace Content.Server.Speech.EntitySystems //ADT-Tweak
{
    //ADT-Tweak-Start
    public sealed class GermanAccentSystem : EntitySystem
    {
        private readonly Dictionary<string, string> _replacements = new();
        private Regex? _replaceRegex;
        //ADT-Tweak-End

        //ADT-Tweak-Start
        // Инструкции для TTS-а, чтобы слова произносились правильно
        private readonly Dictionary<string, string> _pronunciations = new()
        {
            {"wozu", "воцу"},
            {"warum", "ва-рум"},
            {"wie", "ви"},
            {"so", "зо"},
            {"mir", "мир"},
            {"was", "вас"},
            {"ja", "йа"},
            {"nein", "найн"},
            {"sehr", "зер"},
            {"du", "ду"},
            {"stehen", "штеэн"},
            {"wir", "вир"},
            {"schnell", "шнелль"},
            {"mein", "майн"},
            {"meine", "майнэ"},
            {"meiner", "майнэр"},
            {"meines", "майэнс"},
            {"bitte", "би-тэ"},
            {"bitte, bitte sehr", "би-тэ, би-тэ зер"},
            {"entschuldigung", "энтшульдигунг"},
            {"der freund", "дэр фройнт"},
            {"dem freund", "дэм фройнт"},
            {"den freund", "дэн фройнт"},
            {"die freunde", "ди фройндэ"},
            {"kleiner freund", "кляйнэр фройнт"},
            {"die freundin", "ди фройндин"},
            {"der freundin", "дэр фройндин"},
            {"die freundinnen", "ди фройндиннэн"},
            {"kleine freundin", "кляйнэ фройндин"},
            {"kamerad", "камерат"},
            {"zum kameraden", "цум камерадэн"},
            {"bruder", "бру-дэр"},
            {"zum bruder", "цум бру-дэр"},
            {"brüder", "брю-дэр"},
            {"die schwester", "ди швес-тэр"},
            {"der schwester", "дэр швес-тэр"},
            {"die schwestern", "ди швес-тэрн"},
            {"vater", "фа-тэр"},
            {"zum vater", "цум фа-тэр"},
            {"väter", "фэ-тэр"},
            {"gut", "гут"},
            {"guter", "гу-тэр"},
            {"gute", "гу-тэ"},
            {"ausgezeichnet", "аусгэцайх-нэт"},
            {"prächtig", "прэхьтихь"},
            {"prächtiger", "прэхьтих-эр"},
            {"prächtige", "прэхьтихэ"},
            {"prächtigen", "прэхьтих-эн"},
            {"herrlich", "хер-лишь"},
            {"herrlicher", "хер-лишь-эр"},
            {"herrliche", "хер-лишьэ"},
            {"herrlichen", "хер-лишь-эн"},
            {"wunderbar", "вун-дэрбар"},
            {"wunderbarer", "вун-дэрбар-эр"},
            {"wunderbare", "вун-дэрбарэ"},
            {"wunderbaren", "вун-дэрбар-эн"},
            {"schön", "шён"},
            {"schöner", "шёнэр"},
            {"schöne", "шёнэ"},
            {"schönen", "шёнэн"},
            {"assistent", "асси-стент"},
            {"schwein", "швайн"},
            {"dem schwein", "дэм швайн"},
            {"die schweine", "ди швайнэ"},
            {"danke", "данкэ"},
            {"danke schön", "данкэ шён"},
            {"danke vielmals", "данкэ филь-мальс"},
            {"frau", "фрау"},
            {"hey", "хай"},
            {"mensch", "менш"},
            {"hallo", "хаъло"},
            {"guten tag", "гутэн таг"},
            {"guten morgen", "гутэн моргэн"},
            {"guten abend", "гутэн аб-энт"},
            {"gute nacht", "гутэ нахт"},
            {"tschüss", "чюс"},
            {"auf wiedersehen", "ауф видэрзэ-ин"},
            {"clown", "клаун"},
            {"clownchen", "клаун-хэн"},
            {"los", "лос"},
            {"die mannhunde", "ди ман-хундэ"},
            {"den mannhund", "дэн ман-хунд"},
            {"der mannhund", "дэр ман-хунд"},
            {"dem mannhund", "дэм ман-хунд"},
            {"vertilgen", "фэртильгэн"},
            {"verbrennen", "фэрбреннэн"},
            {"töten", "тётэн"},
            {"getötet", "гэтётэт"},
            {"werde töten", "вердэ тётэн"},
            {"bier", "бир"},
            {"wasser", "вас-сэр"},
            {"agent", "агент"},
            {"zum agenten", "цум аген-тэн"},
            {"agenten", "аген-тэн"},
            {"nukleare agenten", "нуклеаре аген-тэн"},
            {"einsatzkräfte", "айн-зацкрэфтэ"},
            {"terrorist", "террорист"},
            {"terroristen", "террористэн"},
            {"korporation", "корпорацион"},
            {"unternehmen", "унтернэмен"},
            {"zum unternehmen", "цум унтернэмен"},
            {"führer", "фюрэр"},
            {"führer'a", "фюрэр'а"},
            {"chief", "чиф"},
            {"chefarzt", "шеф-арцт"},
            {"forschungsleiter", "фор-шунгсляйтэр"},
            {"leiter des personals", "ляйтэр дэс перзональс"},
            {"leiter des sicherheitsdienstes", "ляйтэр дэс зи-хэр-хайтс-динст"},
            {"quartiermeister", "квартирмайстэр"},
            {"polizei", "полицай"},
            {"sicherheit", "зихэрхайт"},
            {"sicherheitsbeamter", "зихэрхайтсбе-амтэр"},
            {"zum sicherheitsbeamten", "цум зихэрхайтсбе-амтэн"},
            {"sicherheitsbeamte", "зихэрхайтсбе-амтэ"},
            {"kadett", "кадет"},
            {"kadetten", "кадет-тэн"},
            {"kadettchen", "кадет-хэн"},
            {"kadettchenen", "кадет-хэн-эн"},
            {"offizier", "оффицир"},
            {"interner affairs agent", "интернэр эффэрс агент"},
            {"magistrat", "магистрат"},
            {"zum magistrat", "цум магистрат"},
            {"magistrate", "магистрат-э"},
            {"blauer schild offizier", "блауэр шильд оффицир"},
            {"zentrales kommando", "централэс ком-ман-до"},
            {"monster", "мон-стэр"},
            {"zum monster", "цум мон-стэр"},
            {"verdammt", "фэрдамт"},
            {"scheiße", "шайсэ"},
            {"arschloch", "аршлох"},
            {"bastard", "бас-тарт"},
            {"bastarde", "бас-тард-э"},
            {"zum bastard", "цум бас-тарт"},
            {"scheiß geil", "шайс гайль"},
            {"verpiss dich", "фэрписс дих"},
            {"hündin", "хюндин"},
            {"dummkopf", "думкопф"},
            {"schwuchtel", "швух-тэль"},
            {"dreckskerl", "дреск-кэрль"},
            {"schurke", "шурк-э"},
            {"zum schurke", "цум шурк-э"},
            {"schurken", "шурк-эн"},
            {"trottel", "трот-тэль"},
            {"wichser", "викс-эр"},
            {"scheißegal", "шай-сэгаль"},
            {"egal", "э-галь"},
            {"zum teufel", "цум тойфэль"}
        };
        //ADT-Tweak-End

        //ADT-Tweak-Start
        // Немецкий акцент
        private Regex? _pronounceRegex;
        //ADT-Tweak-End

        //ADT-Tweak-Start
        public override void Initialize()
        {
            base.Initialize();
            //ADT-Tweak-End

            //ADT-Tweak-Start
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
            _replacements.Add("быстрее", "schnell");

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

            _replacements.Add("друг", "der Freund");
            _replacements.Add("другу", "dem Freund");
            _replacements.Add("друга", "den Freund");
            _replacements.Add("друзья", "die Freunde");
            _replacements.Add("друзей", "die Freunde");
            _replacements.Add("дружок", "kleiner Freund");

            _replacements.Add("подруга", "die Freundin");
            _replacements.Add("подруге", "der Freundin");
            _replacements.Add("подругу", "die Freundin");
            _replacements.Add("подруги", "die Freundinnen");
            _replacements.Add("подруг", "die Freundinnen");
            _replacements.Add("подружка", "kleine Freundin");

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

            _replacements.Add("сестра", "die Schwester");
            _replacements.Add("сестре", "der Schwester");
            _replacements.Add("сестры", "die Schwestern");
            _replacements.Add("сёстры", "die Schwestern");
            _replacements.Add("сестёр", "die Schwestern");

            _replacements.Add("отец", "vater");
            _replacements.Add("отцу", "zum vater");
            _replacements.Add("отца", "vater");
            _replacements.Add("отцы", "väter");
            _replacements.Add("отцов", "väter");

            _replacements.Add("хорошо", "gut");
            _replacements.Add("хорош", "gut");
            _replacements.Add("хороша", "gut");
            _replacements.Add("хороший", "guter");
            _replacements.Add("хорошая", "gute");
            _replacements.Add("хорошие", "gute");
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
            _replacements.Add("свинье", "dem schwein");
            _replacements.Add("свиней", "schwein");
            _replacements.Add("свиньи", "die schweine");

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
            _replacements.Add("клуня", "clownchen");
            _replacements.Add("клуне", "clownchen");
            _replacements.Add("клуни", "clownchen");
            _replacements.Add("клунь", "clownchen");

            _replacements.Add("поехали", "los");
            _replacements.Add("пошли", "los");
            _replacements.Add("давай", "los");
            _replacements.Add("идем", "los");
            _replacements.Add("вперед", "los");
            _replacements.Add("идём", "los");
            _replacements.Add("вперёд", "los");

            _replacements.Add("вульпы", "die mannhunde"); // mannhund дословно - человек-собака
            _replacements.Add("вульп", "den mannhund");
            _replacements.Add("вульпа", "der mannhund");
            _replacements.Add("вульпу", "dem mannhund");
            _replacements.Add("вульпе", "dem mannhund");

            _replacements.Add("истребить", "vertilgen");
            _replacements.Add("сжечь", "verbrennen");

            _replacements.Add("убить", "töten");
            _replacements.Add("убил", "getötet");
            _replacements.Add("убили", "töten");
            _replacements.Add("убью", "werde töten");
            _replacements.Add("убила", "töten");
            _replacements.Add("убьют", "töten");
            _replacements.Add("убейте", "tötet");

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
            _replacements.Add("корпорацию", "korporation");

            _replacements.Add("корпорат", "unternehmen");
            _replacements.Add("корпорату", "zum unternehmen");
            _replacements.Add("корпората", "unternehmen");
            _replacements.Add("корпораты", "unternehmen");
            _replacements.Add("корпоратов", "unternehmen");

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
            _replacements.Add("нра", "forschungsleiter");
            _replacements.Add("нр-а", "forschungsleiter");
            _replacements.Add("нру", "forschungsleiter");
            _replacements.Add("нр-у", "forschungsleiter");
            _replacements.Add("гп", "leiter des personals");
            _replacements.Add("гсб", "leiter des sicherheitsdienstes");
            _replacements.Add("км", "quartiermeister");
            _replacements.Add("кма", "quartiermeister");
            _replacements.Add("км-а", "quartiermeister");
            _replacements.Add("кму", "quartiermeister");
            _replacements.Add("км-у", "quartiermeister");

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
            _replacements.Add("кадетик", "kadettchen");
            _replacements.Add("кадетики", "kadettchenen");
            _replacements.Add("кадетика", "kadettchen");
            _replacements.Add("кадетику", "kadettchen");
            _replacements.Add("кадетиков", "kadettchen");

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
            //ADT-Tweak-End

            //ADT-Tweak-Start
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

            _replacements.Add("пошел нахуй", "verpiss dich");
            _replacements.Add("пошли нахуй", "verpiss dich");
            _replacements.Add("пошёл нахуй", "verpiss dich");
            _replacements.Add("иди нахуй", "verpiss dich");
            _replacements.Add("идите нахуй", "verpiss dich");
            _replacements.Add("пошел ты нахуй", "verpiss dich");
            _replacements.Add("пошли вы нахуй", "verpiss dich");
            _replacements.Add("пошёл ты нахуй", "verpiss dich");
            _replacements.Add("иди ты нахуй", "verpiss dich");
            _replacements.Add("идите вы нахуй", "verpiss dich");
            _replacements.Add("съеби", "verpiss dich");
            _replacements.Add("съебите", "verpiss dich");
            _replacements.Add("съебись", "verpiss dich");
            _replacements.Add("съебитесь", "verpiss dich");

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

            SubscribeLocalEvent<GermanAccentComponent, AccentGetEvent>(OnAccent);
        }

        public string GetPronunciation(string message)
        //ADT-Tweak-End
        {
            //ADT-Tweak-Start
            if (_pronounceRegex == null)
                return message;

            return _pronounceRegex.Replace(message, match =>
            //ADT-Tweak-End
            {
                //ADT-Tweak-Start
                string key = match.Value.ToLowerInvariant();
                if (_pronunciations.TryGetValue(key, out var pron))
                {
                    return pron;
                }
                return match.Value;
            });
            //ADT-Tweak-End
        }

        public string Accentuate(string message) //ADT-Tweak
        {
            //ADT-Tweak-Start
            message = ReplacePunctuation(message);
            return message;
            //ADT-Tweak-End
        }

        private string ReplacePunctuation(string message) //ADT-Tweak
        {
            //ADT-Tweak-Start
            var sentences = AccentSystem.SentenceRegex.Split(message);
            var msg = new StringBuilder();
            foreach (var s in sentences)
            //ADT-Tweak-End
            {
                //ADT-Tweak-Start
                var toInsert = new StringBuilder();
                if (toInsert.Length == 0)
                //ADT-Tweak-End
                {
                    //ADT-Tweak-Start
                    msg.Append(s);
                }
                else
                {
                    msg.Append(s.Insert(s.Length - s.TrimStart().Length, toInsert.ToString()));
                    //ADT-Tweak-End
                }
            }
            //ADT-Tweak-Start
            return msg.ToString();
        }

        private void OnAccent(EntityUid uid, GermanAccentComponent component, AccentGetEvent args)
        {
            var message = args.Message;

            if (_replaceRegex == null)
            //ADT-Tweak-End
            {
                //ADT-Tweak-Start
                args.Message = Accentuate(message);
                return;
                //ADT-Tweak-End
            }

            //ADT-Tweak-Start
            message = _replaceRegex.Replace(message, match =>
            {
                string matchedText = match.Value;
                string key = matchedText.ToLowerInvariant();
                if (!_replacements.TryGetValue(key, out var baseRep))
                    return matchedText;
                //ADT-Tweak-End

                //ADT-Tweak-Start
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
        //ADT-Tweak-End
    }
}
