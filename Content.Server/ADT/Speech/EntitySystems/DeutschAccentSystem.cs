using System.Text.RegularExpressions;
using Content.Server.Speech.Components;


namespace Content.Server.Speech.EntitySystems;

public sealed class DeutschAccentSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DeutschAccentComponent, AccentGetEvent>(OnAccent);
    }

    private void OnAccent(EntityUid uid, DeutschAccentComponent component, AccentGetEvent args)
    {
        var message = args.Message;

        // Changed By Дубик :3 the beginning of Deutsch speech

        message = Regex.Replace(message, "Что", "Was");
        message = Regex.Replace(message, "ЧТО", "WAS");
        message = Regex.Replace(message, "что", "was");

        message = Regex.Replace(message, "Зачем", "Wozu");
        message = Regex.Replace(message, "ЗАЧЕМ", "WOZU");
        message = Regex.Replace(message, "зачем", "wozu");

        message = Regex.Replace(message, "Здравствуйте", "Guten tag");
        message = Regex.Replace(message, "ЗДРАВСТВУЙТЕ", "GUTEN TAG");
        message = Regex.Replace(message, "здравствуйте", "guten tag");

        message = Regex.Replace(message, "Почему", "Warum");
        message = Regex.Replace(message, "ПОЧЕМУ", "WARUM");
        message = Regex.Replace(message, "почему", "warum");

        message = Regex.Replace(message, "Как", "Wie");
        message = Regex.Replace(message, "КАК", "WIE");
        message = Regex.Replace(message, "как", "wie");

        message = Regex.Replace(message, "Так", "So");
        message = Regex.Replace(message, "ТАК", "SO");
        message = Regex.Replace(message, "так", "so");

        message = Regex.Replace(message, "Пожалуйста", "Bitte sehr");
        message = Regex.Replace(message, "ПОЖАЛУЙСТА", "BITTE SEHR");
        message = Regex.Replace(message, "пожалуйста", "bitte sehr");

        message = Regex.Replace(message, "Капитан", "Führer");
        message = Regex.Replace(message, "КАПИТАН", "FUHRER");
        message = Regex.Replace(message, "капитан", "führer");

        message = Regex.Replace(message, "Хорошо", "Gut");
        message = Regex.Replace(message, "ХОРОШО", "GUT");
        message = Regex.Replace(message, "хорошо", "gut");
        message = Regex.Replace(message, "Хороши", "Gut");
        message = Regex.Replace(message, "ХОРОШИ", "GUT");
        message = Regex.Replace(message, "хороши", "gut");

        message = Regex.Replace(message, "Мой", "Mein");
        message = Regex.Replace(message, "МОЙ", "MEIN");
        message = Regex.Replace(message, "мой", "mein");

        message = Regex.Replace(message, "Мое", "Mein");
        message = Regex.Replace(message, "МОЕ", "MEIN");
        message = Regex.Replace(message, "мое", "mein");
        message = Regex.Replace(message, "Моё", "Mein");
        message = Regex.Replace(message, "МОЁ", "MEIN");
        message = Regex.Replace(message, "моё", "mein");

        message = Regex.Replace(message, "Мои", "Meine");
        message = Regex.Replace(message, "МОИ", "MEINE");
        message = Regex.Replace(message, "мои", "meine");

        message = Regex.Replace(message, "Да", "Ja");
        message = Regex.Replace(message, "ДА", "JA");
        message = Regex.Replace(message, "да", "ja");

        message = Regex.Replace(message, "Нет", "Nein");
        message = Regex.Replace(message, "НЕТ", "NEIN");
        message = Regex.Replace(message, "нет", "nein");

        message = Regex.Replace(message, "Отлично", "Ausgezeichnet");
        message = Regex.Replace(message, "ОТЛИЧНО", "AUSGEZEICHNET");
        message = Regex.Replace(message, "отлично", "ausgezeichnet");

        message = Regex.Replace(message, "Восхитительно", "Wunderschoen");
        message = Regex.Replace(message, "ВОСХИТИТЕЛЬНО", "WUNDERSCHOEN");
        message = Regex.Replace(message, "восхитительно", "wunderschoen");

        message = Regex.Replace(message, "Прекрасно", "Schon");
        message = Regex.Replace(message, "ПРЕКРАСНО", "SCHON");
        message = Regex.Replace(message, "прекрасно", "schon");

        message = Regex.Replace(message, "Очень", "Sehr");
        message = Regex.Replace(message, "ОЧЕНЬ", "SEHR");
        message = Regex.Replace(message, "очень", "sehr");

        message = Regex.Replace(message, "Ассистент", "Assistent");
        message = Regex.Replace(message, "АССИСТЕНТ", "ASSISTENT");
        message = Regex.Replace(message, "ассистент", "assistent");

        message = Regex.Replace(message, "Ассистуха", "Assistent");
        message = Regex.Replace(message, "АССИСТУХА", "ASSISTENT");
        message = Regex.Replace(message, "ассистуха", "assistent");

        message = Regex.Replace(message, "Свинья", "Schwein");
        message = Regex.Replace(message, "СВИНЬЯ", "SCHWEIN");
        message = Regex.Replace(message, "свинья", "schwein");

        message = Regex.Replace(message, "Ты", "Du");
        message = Regex.Replace(message, "ТЫ", "DU");
        message = Regex.Replace(message, "ты", "du");

        message = Regex.Replace(message, "Спасибо", "Danke");
        message = Regex.Replace(message, "СПАСИБО", "DANKE");
        message = Regex.Replace(message, "спасибо", "danke");

        message = Regex.Replace(message, "Женщина", "Frau");
        message = Regex.Replace(message, "ЖЕНЩИНА", "FRAU");
        message = Regex.Replace(message, "женщина", "frau");

        message = Regex.Replace(message, "Эй", "Hey");
        message = Regex.Replace(message, "ЭЙ", "HEY");
        message = Regex.Replace(message, "эй", "hey");

        message = Regex.Replace(message, "Человек", "Mensch");
        message = Regex.Replace(message, "ЧЕЛОВЕК", "MENSCH");
        message = Regex.Replace(message, "человек", "mensch");

        message = Regex.Replace(message, "Стоять", "Stehen");
        message = Regex.Replace(message, "СТОЯТЬ", "STEHEN");
        message = Regex.Replace(message, "стоять", "stehen");

        message = Regex.Replace(message, "Привет", "Hallo");
        message = Regex.Replace(message, "ПРИВЕТ", "HALLO");
        message = Regex.Replace(message, "привет", "hallo");

        message = Regex.Replace(message, "Сб", "Polizei");  
        message = Regex.Replace(message, "СБ", "POLIZEI");  
        message = Regex.Replace(message, "сб", "polizei");

        message = Regex.Replace(message, "Си", "Chief");  
        message = Regex.Replace(message, "СИ", "Chief");  
        message = Regex.Replace(message, "си", "chief");

        message = Regex.Replace(message, "ГВ", "Chefarzt");  
        message = Regex.Replace(message, "Гв", "Chefarzt");  
        message = Regex.Replace(message, "гв", "chefarzt");  

        message = Regex.Replace(message, "НР", "Doktorvater");  
        message = Regex.Replace(message, "Нр", "Doktorvater");  
        message = Regex.Replace(message, "нр", "doktorvater"); 

        message = Regex.Replace(message, "Капитан", "Führer");
        message = Regex.Replace(message, "КАПИТАН", "FUHRER");
        message = Regex.Replace(message, "капитан", "führer");

        message = Regex.Replace(message, "Капитана", "Führer'a");
        message = Regex.Replace(message, "КАПИТАНА", "FUHRER'A");
        message = Regex.Replace(message, "капитана", "führer'a");

        message = Regex.Replace(message, "Кеп", "Führer");
        message = Regex.Replace(message, "КЕП", "FUHRER");
        message = Regex.Replace(message, "кеп", "führer");

        message = Regex.Replace(message, "Кепа", "Führer'a");
        message = Regex.Replace(message, "КЕПА", "FUHRER'A");
        message = Regex.Replace(message, "кепа", "führer'a");

        message = Regex.Replace(message, "Мы", "Wir");
        message = Regex.Replace(message, "МЫ", "WIR");
        message = Regex.Replace(message, "мы", "wir");

        message = Regex.Replace(message, "Кадет", "Kadett");
        message = Regex.Replace(message, "КАДЕТ", "KADETT");
        message = Regex.Replace(message, "кадеты", "kadett");

        message = Regex.Replace(message, "Офицер", "Offizier");
        message = Regex.Replace(message, "ОФИЦЕР", "OFFIZIER");
        message = Regex.Replace(message, "офицер", "offizier");

        message = Regex.Replace(message, "Кадеты", "Kadetten");
        message = Regex.Replace(message, "КАДЕТЫ", "KADETTEN");
        message = Regex.Replace(message, "кадеты", "kadetten");

        message = Regex.Replace(message, "Клоун", "Clown");
        message = Regex.Replace(message, "КЛОУН", "CLOWN");
        message = Regex.Replace(message, "клоун", "clown");
        message = Regex.Replace(message, "Клоуна", "Clown'a");
        message = Regex.Replace(message, "КЛОУНА", "CLOWN'A");
        message = Regex.Replace(message, "клоуна", "clown'a");

        message = Regex.Replace(message, "Вульпа", "Vulpa");
        message = Regex.Replace(message, "ВУЛЬПА", "VULPA");
        message = Regex.Replace(message, "вульпа", "vulpa");

        message = Regex.Replace(message, "Вульп", "Vulp");
        message = Regex.Replace(message, "ВУЛЬП", "VULP");
        message = Regex.Replace(message, "вульп", "vulp");

        message = Regex.Replace(message, "Истребить", "Vertilgen");
        message = Regex.Replace(message, "ИСТРЕБИТЬ", "VERTIGEN");
        message = Regex.Replace(message, "истребить", "vertilgen");

        message = Regex.Replace(message, "Сжечь", "Verbrennen");
        message = Regex.Replace(message, "СЖЕЧЬ", "VERBRENNEN");
        message = Regex.Replace(message, "сжечь", "verbrennen");

        message = Regex.Replace(message, "Убить", "Töten");
        message = Regex.Replace(message, "УБИТЬ", "TOTEN");
        message = Regex.Replace(message, "убить", "töten");

        message = Regex.Replace(message, "Убили", "Töten");
        message = Regex.Replace(message, "УБИЛИ", "TOTEN");
        message = Regex.Replace(message, "убили", "töten");

        message = Regex.Replace(message, "Убейте", "Töten");
        message = Regex.Replace(message, "УБЕЙТЕ", "TOTEN");
        message = Regex.Replace(message, "убейте", "töten");

        message = Regex.Replace(message, "Пиво", "Bier");
        message = Regex.Replace(message, "ПИВО", "BIER");
        message = Regex.Replace(message, "пиво", "bier");

        message = Regex.Replace(message, "Пива", "Bier");
        message = Regex.Replace(message, "ПИВА", "BIER");
        message = Regex.Replace(message, "пива", "bier");

        message = Regex.Replace(message, "Вода", "Wasser");
        message = Regex.Replace(message, "ВОДА", "WASSER");
        message = Regex.Replace(message, "вода", "wasser");

        message = Regex.Replace(message, "Воды", "Wasser");
        message = Regex.Replace(message, "ВОДЫ", "WASSER");
        message = Regex.Replace(message, "воды", "wasser");

        message = Regex.Replace(message, "ГП", "Leiter des Personals");
        message = Regex.Replace(message, "Гп", "Leiter des Personals");
        message = Regex.Replace(message, "гп", "leiter des personals");

        message = Regex.Replace(message, "ГСБ", "Leiter des Sicherheitsdienstes");
        message = Regex.Replace(message, "Глава Службы Безопасности", "Leiter des Sicherheitsdienstes");
        message = Regex.Replace(message, "гсб", "leiter des sicherheitsdienstes");

        message = Regex.Replace(message, "КМ", "Quartiermeister");
        message = Regex.Replace(message, "Км", "Quartiermeister");
        message = Regex.Replace(message, "Квартирмейстер", "Quartiermeister");
        message = Regex.Replace(message, "км", "quartiermeister");

        message = Regex.Replace(message, "ЯО", "Terroristen");
        message = Regex.Replace(message, "Яо", "Terroristen");
        message = Regex.Replace(message, "Ядерные оперативники", "Terroristen");
        message = Regex.Replace(message, "яо", "terroristen");

        // оскорбления

        message = Regex.Replace(message, "Похуй", "Scheib");
        message = Regex.Replace(message, "похуй", "scheib");
        message = Regex.Replace(message, "Похую", "Scheib");
        message = Regex.Replace(message, "похую", "scheib");

        message = Regex.Replace(message, "Пошел нахуй", "Leck mich");
        message = Regex.Replace(message, "пошел нахуй", "leck mich");
        message = Regex.Replace(message, "Пошли нахуй", "Leck mich");
        message = Regex.Replace(message, "пошли нахуй", "leck mich");

        message = Regex.Replace(message, "Блять", "Scheibe");
        message = Regex.Replace(message, "БЛЯТЬ", "SCHEIBE");
        message = Regex.Replace(message, "блять", "scheibe");

        message = Regex.Replace(message, "Бля", "Scheibe");
        message = Regex.Replace(message, "БЛЯ", "SCHEIBE");
        message = Regex.Replace(message, "бля", "scheibe");

        message = Regex.Replace(message, "Сука", "Hündin");
        message = Regex.Replace(message, "СУКА", "HUNDIN");
        message = Regex.Replace(message, "сука", "hündin");

        message = Regex.Replace(message, "Идиот", "Dummkopf");
        message = Regex.Replace(message, "ИДИОТ", "DUMMKOPF");
        message = Regex.Replace(message, "идиот", "dummkopf");

        message = Regex.Replace(message, "Идиоты", "Dummkopf");
        message = Regex.Replace(message, "ИДИОТЫ", "DUMMKOPF");
        message = Regex.Replace(message, "идиоты", "dummkopf");

        message = Regex.Replace(message, "Пидор", "Arschloch");
        message = Regex.Replace(message, "ПИДОР", "ARSCHLOCH");
        message = Regex.Replace(message, "пидор", "arschloch");

        message = Regex.Replace(message, "Пидорас", "Schwuchtel");
        message = Regex.Replace(message, "ПИДОРАС", "SCHWUCHTEL");
        message = Regex.Replace(message, "пидорас", "schwuchtel");

        message = Regex.Replace(message, "Мразь", "Dreckskerl");
        message = Regex.Replace(message, "МРАЗЬ", "DRECKSKERL");
        message = Regex.Replace(message, "мразь", "dreckskerl");

        message = Regex.Replace(message, "Еблан", "Ficker");
        message = Regex.Replace(message, "ЕБЛАН", "FICKER");
        message = Regex.Replace(message, "еблан", "ficker");

        message = Regex.Replace(message, "Уебок", "Wichser");
        message = Regex.Replace(message, "УЕБОК", "WICHSER");
        message = Regex.Replace(message, "уебок", "wichser");

        message = Regex.Replace(message, "Уёбок", "Wichser");
        message = Regex.Replace(message, "УЁБОК", "WICHSER");
        message = Regex.Replace(message, "уёбок", "wichser");

        message = Regex.Replace(message, "Нахуя", "Fick dich");
        message = Regex.Replace(message, "НАХУЯ", "FICK DICH");
        message = Regex.Replace(message, "нахуя", "fick dich");

        message = Regex.Replace(message, "Ебланище", "Scheißkerl");
        message = Regex.Replace(message, "ЕБЛАНИЩЕ", "SCHEIBKERL");
        message = Regex.Replace(message, "ебланище", "scheißkerl");

        // Changed By Дубик :3 the stop of Deutsch speech

        args.Message = message;
    }
}