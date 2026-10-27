ent-ADTEasel = мольберт
    .desc = Деревянная подставка для холста. Поставьте на неё холст и рисуйте.
ent-ADTCanvasBase = холст
    .desc = Холст, натянутый на деревянную раму. Поставьте его на мольберт, чтобы рисовать.
ent-ADTCanvasSmall = маленький холст
    .desc = { ent-ADTCanvasBase.desc }
ent-ADTCanvasPixelSmall = маленький холст для пиксель-арта
    .desc = Холст 32×32 для пиксель-арта. Поставьте его на мольберт, чтобы рисовать.
ent-ADTCanvasPixel = холст для пиксель-арта
    .desc = Холст 64×64 для пиксель-арта. Поставьте его на мольберт, чтобы рисовать.
ent-ADTCanvasLarge = большой холст
    .desc = { ent-ADTCanvasBase.desc }
ent-ADTCanvasHuge = огромный холст
    .desc = { ent-ADTCanvasBase.desc }

adt-canvas-easel-slot = холст

adt-canvas-easel-title = Мольберт
adt-canvas-easel-canvas = Холст {$width}×{$height}
adt-canvas-easel-no-canvas = Поставьте холст на мольберт.
adt-canvas-easel-busy = Сейчас рисует: {$name}
adt-canvas-easel-mine = Вы рисуете на этом холсте.
adt-canvas-easel-foreign = На этом холсте уже рисовал кто-то другой.
adt-canvas-easel-start = Начать рисовать
adt-canvas-easel-continue = Продолжить
adt-canvas-easel-finish = Завершить и сохранить
adt-canvas-easel-drafts = Черновики ({$count}/{$slots})
adt-canvas-easel-drafts-hint = Черновик можно продолжить на чистом холсте того же размера.
adt-canvas-easel-drafts-full = Все слоты заняты: новая работа вытеснит самый старый черновик.
adt-canvas-easel-untitled = Без названия
adt-canvas-easel-draft = {$title} · {$width}×{$height} · {$date}
adt-canvas-easel-draft-open = Продолжить
adt-canvas-easel-draft-size = Нужен чистый холст того же размера.

adt-canvas-error-disabled = Рисование сейчас отключено.
adt-canvas-error-busy = На этом холсте уже рисуют.
adt-canvas-error-no-canvas = На мольберте нет холста.
adt-canvas-error-generic = Не удалось открыть холст.
adt-canvas-error-server-limit = Сейчас рисуют слишком многие, попробуйте позже.
adt-canvas-error-player-limit = Вы уже рисуете на другом холсте.
adt-canvas-error-not-owner = Это чужая работа.
adt-canvas-error-draft-missing = Черновик не найден или уже недоступен.
adt-canvas-error-draft-size = Размер черновика не совпадает с холстом.

adt-canvas-sync-loading = Загрузка холста: {$loaded}/{$total}
adt-canvas-sync-error-corrupt = Часть холста повреждена, рисование остановлено.
adt-canvas-sync-error-too-large = Картина стала слишком тяжёлой, изменения больше не сохраняются.
adt-canvas-sync-error-not-allowed = Сессия рисования закрыта, изменения больше не сохраняются.
adt-canvas-sync-error-invalid = Сервер отклонил изменения холста.

adt-drawing-finish = Завершить и сохранить

adt-canvas-role-host = хост
adt-canvas-role-editor = соавтор
adt-canvas-role-viewer = зритель

adt-canvas-session-header = Сессия
adt-canvas-session-participant = {$name} · {$role}{$self ->
    [1] {" "}(вы)
   *[other] {""}
}{$away ->
    [1] {" "}· не у мольберта
   *[other] {""}
}
adt-canvas-session-request = {$name} просится в соавторы
adt-canvas-session-accept = Принять
adt-canvas-session-reject = Отклонить
adt-canvas-session-decline = Отказаться
adt-canvas-session-invite = Пригласить
adt-canvas-session-invited = {$name} приглашает вас рисовать вместе.
adt-canvas-session-join = Попроситься в соавторы
adt-canvas-session-request-sent = Заявка отправлена, ждём ответа хоста.
adt-canvas-session-cancel = Отозвать заявку
adt-canvas-session-make-host = Сделать хостом
adt-canvas-session-kick = Исключить
adt-canvas-session-confirm = Точно?
adt-canvas-session-leave = Выйти из соавторов
adt-canvas-session-leave-wait = Подождите, пока сохранятся последние штрихи.
adt-canvas-session-revert = Откатить к сохранению
adt-canvas-session-viewer = Вы смотрите, как рисует {$name}. Попроситесь в соавторы, чтобы рисовать вместе.

adt-canvas-join-blocked = Хост уже трижды отказал вам, до конца смены заявки на этот холст закрыты.
adt-canvas-join-cooldown = Подождите немного перед новой заявкой.
adt-canvas-join-full = Соавторов уже максимум.
adt-canvas-join-requested = {$name} просится к вам в соавторы.
adt-canvas-join-sent = Заявка отправлена хосту.
adt-canvas-join-rejected = Хост отклонил вашу заявку.
adt-canvas-join-accepted = Теперь вы соавтор и можете рисовать.
adt-canvas-invite-received = {$name} приглашает вас в соавторы.
adt-canvas-invite-declined = {$name} не принимает приглашение.
adt-canvas-kicked = Хост исключил вас из соавторов.
adt-canvas-kicked-flood = Вы отправляли слишком много штрихов и исключены из соавторов.
adt-canvas-host-busy = Этот игрок уже хостит другой холст.
adt-canvas-host-received = Теперь вы хост этого холста.
adt-canvas-revert-nothing = С последнего сохранения ничего не менялось.
adt-canvas-revert-done = Холст откачен к последнему сохранению.
adt-canvas-error-ghost = Вы не можете рисовать в таком виде.
adt-canvas-error-parent = Не удалось продолжить эту картину: нужна своя одобренная работа и чистый холст того же размера.
adt-canvas-easel-approved = Одобренные работы
adt-canvas-easel-continue-new = Продолжить как новую

adt-canvas-rules-title = Правила рисования
adt-canvas-rules-text =
    { "" }[bold]Прежде чем взяться за кисть, прочитайте правила.[/bold]

    Каждая картина проходит проверку администрацией. Работы с нарушениями отклоняются, а их автор может лишиться доступа к рисованию.

    { "" }[bold]Запрещено рисовать:[/bold]
    - любую символику: гербы, эмблемы, знаки организаций и движений;
    - флаги любых государств, регионов и сообществ. Единственное исключение - флаг США;
    - контент 18+: обнажённую натуру, откровенные сцены и любой эротический подтекст, в том числе намёками;
    - символику ненависти и экстремизма, а также её стилизации и переделки;
    - отсылки к реальным конфликтам и трагическим событиям, к реальным странам, их политике и политикам.

    Если сомневаетесь, можно ли такое рисовать, лучше не рисуйте.
