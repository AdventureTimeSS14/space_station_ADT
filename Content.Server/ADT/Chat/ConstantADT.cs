namespace Content.Server.ADT.Chat;
public static class ChatFilterConstants
{
    public static readonly HashSet<string> OffensiveWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "Мама", "Мамы", "Маме", "Маму", "Мамой",
        "Мать", "Матери", "Матерью",
        "Мамка", "Мамке", "Мамку", "Мамкой",
        "Мамаша", "Мамаши", "Мамаше", "Мамашей",
        "Папа", "Папы", "Папу", "Папе", "Папой",
        "Отец", "Отца", "Отцу", "Отцом",
        "Папаша", "Папаши", "Папаше", "Папашей", "Папашу",
        "Шлюха", "Шлюхи", "Шлюхе", "Шлюхой"
    };
}
