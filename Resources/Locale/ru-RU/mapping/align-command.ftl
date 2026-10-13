cmd-align-desc =
    Автоматически выравнивает все закреплённые шлюзы, двери, пожарные шлюзы и т.п.
    по соседним конструкциям.
    Параметр [dry run] выполняет проверку без поворота.
cmd-align-help = Использование: { $command } [MapID] [dry run?]
cmd-align-no-release = Эту команду нельзя использовать, если игра запущена в конфигурации RELEASE.
cmd-align-hint-id = MapID
cmd-align-hint-dry = dry run?
cmd-align-feedback-none =
    { $dry ->
        [true] ПРОБНЫЙ ЗАПУСК: Не найдено
       *[false] Не найдено
    } сущностей, совместимых с AlignerSystem!
cmd-align-feedback-good =
    { $dry ->
        [true] ПРОБНЫЙ ЗАПУСК: Не найдено
       *[false] Не найдено
    } невыровненных сущностей.
cmd-align-feedback =
    { $dry ->
        [true] ПРОБНЫЙ ЗАПУСК: Найдено
       *[false] Найдено и исправлено
    } невыровненных сущностей: { $fixed }.
