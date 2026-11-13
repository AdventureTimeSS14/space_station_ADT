adt-poll-escape-button = Опросы
adt-poll-escape-button-count = Опросы ({ $count })

adt-poll-invite-title = Новый опрос
adt-poll-invite-text = Нам важно ваше мнение. Пожалуйста, пройдите опрос:
adt-poll-invite-open = Пройти
adt-poll-invite-later = Позже

adt-poll-list-title = Опросы
adt-poll-list-empty = Сейчас нет активных опросов.
adt-poll-list-targeted = Для вас
adt-poll-list-ends = До { $date }
adt-poll-list-open = Открыть
adt-poll-list-answered = Пройден

adt-poll-answer-submit = Отправить
adt-poll-answer-text-placeholder = Ваш ответ
adt-poll-submit-success = Спасибо, ваш ответ сохранён.

adt-poll-error-closed = Опрос уже закрыт.
adt-poll-error-already-answered = Вы уже прошли этот опрос.
adt-poll-error-answers-count = Ответы не совпадают с вопросами, откройте опрос заново.
adt-poll-error-bad-answer = Некорректный ответ.
adt-poll-error-required = Ответьте на все обязательные вопросы.
adt-poll-error-text-too-long = Текстовый ответ слишком длинный.
adt-poll-error-save = Не удалось сохранить, подробности в логе сервера.
adt-poll-error-no-title = Укажите заголовок опроса.
adt-poll-error-too-long = Слишком длинный текст.
adt-poll-error-no-questions = Добавьте хотя бы один вопрос.
adt-poll-error-too-many-questions = Слишком много вопросов.
adt-poll-error-question-text = У каждого вопроса должен быть текст.
adt-poll-error-options-count = У вопроса с вариантами должно быть от 2 до 20 вариантов.
adt-poll-error-rating-range = Шкала оценки задана неверно: "от" должно быть меньше "до", а разница не больше 10.
adt-poll-error-question-type = Неизвестный тип вопроса.
adt-poll-error-negative-hours = Часы не могут быть отрицательными.
adt-poll-error-not-found = Опрос не найден.

adt-poll-mode-immediately = Сразу
adt-poll-mode-round-start = В начале раунда
adt-poll-mode-round-end = В конце раунда

adt-poll-type-single = Один вариант
adt-poll-type-multiple = Несколько вариантов
adt-poll-type-rating = Оценка
adt-poll-type-text = Свободный текст

adt-poll-antag-any = Все
adt-poll-antag-only = Только антагонисты
adt-poll-antag-exclude = Без антагонистов

adt-poll-panel-title = Опросы
adt-poll-panel-no-permission = Нужен флаг Host.
adt-poll-panel-tab-polls = Опросы
adt-poll-panel-tab-editor = Редактор
adt-poll-panel-new = Новый
adt-poll-panel-edit = Изменить
adt-poll-panel-stats = Статистика
adt-poll-panel-close = Закрыть
adt-poll-panel-open = Открыть
adt-poll-panel-wipe = Стереть ответы
adt-poll-panel-delete = Удалить
adt-poll-panel-confirm = Точно?
adt-poll-panel-refresh = Обновить
adt-poll-panel-save = Сохранить
adt-poll-panel-saved = Опрос сохранён.
adt-poll-panel-saved-questions-locked = Опрос сохранён. Вопросы не изменены, потому что на опрос уже отвечали.
adt-poll-panel-done = Готово.
adt-poll-panel-stats-empty = Выберите опрос и нажмите "Статистика".
adt-poll-panel-state-active = активен
adt-poll-panel-state-closed = закрыт
adt-poll-panel-no-end = без срока
adt-poll-panel-all-servers = все серверы
adt-poll-panel-details = Показ: { $mode }
    Окончание: { $ends }
    Серверы: { $servers }
    Ответов: { $responses }, ждут ответа: { $pending }

adt-poll-editor-new = Новый опрос
adt-poll-editor-edit = Опрос #{ $id }
adt-poll-editor-title = Заголовок
adt-poll-editor-description = Описание
adt-poll-editor-show = Показ
adt-poll-editor-mode = Когда показать
adt-poll-editor-hours = Длится часов с момента сохранения
adt-poll-editor-no-end = Без срока
adt-poll-editor-ends-current = Сейчас заканчивается { $date }. Оставьте поле пустым, чтобы не менять срок.
adt-poll-editor-servers = Серверы
adt-poll-editor-servers-placeholder = Через запятую, пусто - все серверы
adt-poll-editor-server-current = Этот сервер: { $server }
adt-poll-editor-filters = Кому показывать
adt-poll-editor-require-round = Только участникам раунда
adt-poll-editor-jobs = Должности
adt-poll-editor-departments = Отделы
adt-poll-editor-species = Расы
adt-poll-editor-antag = Антагонисты
adt-poll-editor-antag-roles = Роли антагонистов
adt-poll-editor-playtime = Общее время, часов (от - до)
adt-poll-editor-tracker = Время на трекере, часов (от - до)
adt-poll-editor-tracker-none = Не учитывать
adt-poll-editor-questions = Вопросы
adt-poll-editor-locked = На опрос уже отвечали, вопросы менять нельзя.
adt-poll-editor-add-question = Добавить вопрос
adt-poll-editor-question-placeholder = Текст вопроса
adt-poll-editor-required = Обязательный
adt-poll-editor-options = Варианты, по одному в строке
adt-poll-editor-rating-from = От
adt-poll-editor-rating-to = до

adt-poll-stats-summary = { $title }: ответов { $total } (адресаты { $targeted }, сами { $voluntary })
adt-poll-stats-group-by-none = Без разбивки
adt-poll-stats-group-by-department = По отделам
adt-poll-stats-group-by-job = По должностям
adt-poll-stats-group-by-species = По расам
adt-poll-stats-group-none = Не указано
adt-poll-stats-question = { $index }. { $text } (ответов: { $answered })
adt-poll-stats-text-below = Текстовые ответы смотрите ниже.
adt-poll-stats-average = Средняя оценка: { $value }
adt-poll-stats-group-rating = средняя { $value }, ответов { $count }
adt-poll-stats-responses = Ответы
adt-poll-stats-response-header = { $name } - { $date } - раунд { $round } - { $server } - { $kind }
adt-poll-stats-targeted = адресат
adt-poll-stats-voluntary = сам
adt-poll-stats-not-in-round = не участвовал в раунде
adt-poll-stats-playtime = { $hours } ч
adt-poll-stats-no-answer = без ответа
