## Команда: banmass
cmd-banmass-desc = Банит сразу нескольких игроков по никнейму или ID.
cmd-banmass-help = Использование: banmass "Причина" <время> name1 name2 name3.. Время - продолжительность в минутах, 0 для пермабана.

## Команда: kick_hide
cmd-kick_hide-desc = Тихо кикает пользователя разрывая соединение.
cmd-kick_hide-help = Использование: kick_hide <nickname>
cmd-kick_hide-player = Сессия игрока не найдена.
cmd-kick_hide-error-arg = Не указан аргумент для команды.

## Команда: echo_chat
cmd-echo_chat-desc = Заставляет указанного игрока или сущности постить ваше message сообщение, если сущность имеет такую возможность.
cmd-echo_chat-help = Использование: echo_chat <userName/uid(сущности)> <message> <speak/emote/whisper>
echo_chat-hint = <userName/uid>
echo_chat-why-help = Тип постинга чата
echo_chat-message-help = Ваше сообщение для постинга сущностью "Ваше сообщение"
echo_chat-speak-help = Использовать обычную разговорную речь
echo_chat-emote-help = Использовать чат эмоций
echo_chat-whisper-help = Использование чата шёпотом
echo_chat-whisper-error-args = Ошибка: неверное количество аргументов! Ожидается 3 аргумента: <Никнейм/Uid> <Текст> <Тип чата>

## Команда: admin_toggle
cmd-admin_toggle-desc = Вводит в деадмин или реадмин указанного пользователя.
cmd-admin_toggle-help = Использование: admin_toggle <userName> <readmin/deadmin>
cmd-admin_toggle-hint-duration = Значение
cmd-admin_toggle-readmin = Вернуть права админу.
cmd-admin_toggle-deadmin = Ввести в деадмин.
cmd-admin_toggle-error-args = Указанный пользователь не найден.

# Команда: export
cmd-export-only-yml = Указанный файл должен иметь расширение .yml
cmd-export-help = Экспортирует на ваш компьютер указанный .yml файл

# Команда: lslawset_get
cmd-lslawset_get-desc = Выводит список законов у сущности которая имеет SiliconLawProviderComponent.
cmd-lslawset_get-error-component = Сущность не имеет SiliconLawProviderComponent.
cmd-lslawset_get-help = Использование: lslawset_get <userName/Uid>

# Команда: setmind_swap
set-mind-swap-command-description = Меняет местами сознания указанных сущностей. Сущности должна иметь { $requiredComponent }.
set-mind-swap-command-help-text = Использование: { $command } <entityUid1> <entityUid2> [unvisit]
set-mind-swap-success-message = Сознания успешно поменялись между собой.
set-mind-swap-command-minds-not-found = Ошибка: сознания не найдены или сущности некорректны.
