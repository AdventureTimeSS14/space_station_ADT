ent-ADTPainting = картина
    .desc = Готовая картина. Повесьте её в рамку.
ent-ADTPaintingFrameBase = рамка для картины
    .desc = Рамка для готовой картины. Повесьте её на стену и вставьте картину.
ent-ADTPaintingFrame = рамка для картины
    .desc = { ent-ADTPaintingFrameBase.desc }
ent-ADTPaintingFrameIron = железная рамка для картины
    .desc = { ent-ADTPaintingFrameBase.desc }
ent-ADTPaintingFrameBamboo = бамбуковая рамка для картины
    .desc = { ent-ADTPaintingFrameBase.desc }
ent-ADTPaintingFrameNecropolis = рамка некрополя
    .desc = { ent-ADTPaintingFrameBase.desc }
ent-ADTPaintingFrameGallery = { ent-ADTPaintingFrame }
    .desc = { ent-ADTPaintingFrameBase.desc }
    .suffix = Галерея
ent-ADTPaintingFrameGalleryPinned = { ent-ADTPaintingFrame }
    .desc = { ent-ADTPaintingFrameBase.desc }
    .suffix = Галерея, закреплённые

adt-painting-frame-slot = картина
adt-painting-verb-view = Рассмотреть картину

adt-painting-viewer-title = Картина
adt-painting-viewer-loading = Загрузка...
adt-painting-viewer-denied = Картина недоступна.
adt-painting-viewer-status = {$width}×{$height} · {$status} · колесо - масштаб, перетаскивание - сдвиг

adt-painting-status-draft = черновик
adt-painting-status-submitted = на модерации
adt-painting-status-approved = одобрена
adt-painting-status-rejected = отклонена
adt-painting-status-hidden = скрыта
adt-painting-status-deleted = удалена
adt-painting-status-expired = просрочена

adt-drawing-submit = Отправить на сохранение

adt-canvas-easel-pending = На модерации
adt-canvas-easel-withdraw = Отозвать

adt-canvas-submit-title = Отправка на сохранение
adt-canvas-submit-name = Название (до 64 символов)
adt-canvas-submit-description = Описание (необязательно, до 256 символов)
adt-canvas-submit-send = Отправить
adt-canvas-submit-cancel = Отмена
adt-canvas-submit-merge-failed = Не удалось влить слои в холст. Закончите штрих и отправьте работу ещё раз.
adt-canvas-submit-waiting = Видимые слои влиты в холст. Работа отправится, как только сохранятся изменения.
adt-canvas-submit-hint = Видимые слои будут влиты в холст. Работу проверит администрация. После отправки её нельзя рисовать дальше, но до решения её можно отозвать на мольберте.
adt-canvas-submit-done = Картина отправлена на модерацию.
adt-canvas-submit-error-title = Укажите название картины (до 64 символов).
adt-canvas-submit-error-description = Описание слишком длинное.
adt-canvas-submit-error-cooldown = Отправлять картины можно не чаще раза в 10 минут.
adt-canvas-submit-error-pending = У вас уже слишком много картин на модерации.
adt-canvas-submit-error-approved = Достигнут лимит сохранённых картин.
adt-canvas-submit-error-broken = Картину не удалось обработать, она возвращена в черновики.
adt-canvas-withdraw-done = Картина отозвана и снова стала черновиком.
adt-canvas-withdraw-failed = Картину не удалось отозвать: возможно, решение уже принято.

adt-canvas-error-banned = Вам запрещено рисовать.
adt-canvas-error-not-whitelisted = Рисование доступно только игрокам из вайтлиста.
adt-canvas-error-submitted = Эта работа на модерации. Отзовите её на мольберте, чтобы продолжить.
adt-canvas-error-approved = Эта работа уже сохранена, её нельзя изменять.

adt-canvas-decision-approved = Ваша картина «{$title}» одобрена и сохранена.
adt-canvas-decision-rejected = Ваша картина «{$title}» отклонена. Причина: {$reason}

adt-canvas-mod-no-access = Недостаточно прав.
adt-canvas-mod-reason-required = Укажите причину.
adt-canvas-mod-not-found = Картина не найдена.
adt-canvas-mod-already-reviewed = По этой картине уже принято решение.
adt-canvas-mod-approved = Картина «{$title}» одобрена.
adt-canvas-mod-rejected = Картина «{$title}» отклонена.
adt-canvas-mod-hidden = Картина «{$title}» скрыта.
adt-canvas-mod-unhidden = Картина «{$title}» снова видна.
adt-canvas-mod-deleted = Картина «{$title}» удалена.
adt-canvas-mod-pinned = Картина закреплена для галерей.
adt-canvas-mod-unpinned = Картина откреплена.
adt-canvas-mod-spawned = Картина «{$title}» создана под вами.
adt-canvas-mod-spawn-not-approved = Заспавнить можно только одобренную картину.
adt-canvas-mod-spawn-no-entity = Вы не находитесь в мире.
adt-canvas-mod-banned = Игроку запрещено рисовать.
adt-canvas-mod-unbanned = Запрет на рисование снят.
adt-canvas-mod-not-banned = У игрока нет запрета на рисование.
adt-canvas-mod-whitelist-added = Игрок {$name} добавлен в вайтлист рисования.
adt-canvas-mod-whitelist-exists = Игрок {$name} уже в вайтлисте рисования.
adt-canvas-mod-whitelist-removed = Игрок {$name} убран из вайтлиста рисования.
adt-canvas-mod-whitelist-missing = Игрока {$name} нет в вайтлисте рисования.
adt-canvas-mod-failed = Не удалось выполнить действие, см. лог сервера.
adt-canvas-mod-new-submission = Новая картина на модерации: «{$title}». Всего в очереди: {$count}. Команда paintingqueue.
adt-canvas-mod-storage-warning = Галерея картин занимает {$used} МБ из {$limit} МБ.

adt-canvas-queue-title = Модерация картин
adt-canvas-queue-filter-submitted = На модерации
adt-canvas-queue-filter-approved = Одобренные
adt-canvas-queue-filter-hidden = Скрытые
adt-canvas-queue-filter-rejected = Отклонённые
adt-canvas-queue-refresh = Обновить
adt-canvas-queue-storage = Галерея: {$used} МБ
adt-canvas-queue-storage-limit = Галерея: {$used}/{$limit} МБ
adt-canvas-queue-entry = {$title} · {$owner} · {$date}
adt-canvas-queue-empty = Выберите картину в списке.
adt-canvas-queue-thumb-loading = Превью загружается...
adt-canvas-queue-open = Открыть
adt-canvas-queue-approve = Одобрить
adt-canvas-queue-reject = Отклонить
adt-canvas-queue-reason = Причина
adt-canvas-queue-hide = Скрыть
adt-canvas-queue-unhide = Вернуть
adt-canvas-queue-pin = Закрепить
adt-canvas-queue-unpin = Открепить
adt-canvas-queue-delete = Удалить
adt-canvas-queue-delete-confirm = Точно удалить?
adt-canvas-queue-spawn = Заспавнить
adt-canvas-queue-ban-minutes = Минуты, 0 - навсегда
adt-canvas-queue-ban = Бан на рисование
adt-canvas-queue-unban = Снять бан
adt-canvas-queue-banned = , есть бан на рисование
adt-canvas-queue-details =
    {$title} · {$width}×{$height} · {$status}
    Автор: {$owner} (одобрено {$approved}, отклонено {$rejected}{$banned})
    Соавторы: {$contributors}
    Раунд: {$round} · отправлена {$submitted}
    Решение: {$reviewed}, {$reviewer}, комментарий: {$comment}
    Вес: {$size} КБ
    Описание: {$description}

adt-painting-report-verb = Пожаловаться на картину
adt-painting-report-title = Жалоба на картину
adt-painting-report-prompt = Что не так с картиной?
adt-painting-report-sent = Жалоба отправлена администрации.
adt-painting-report-already = Вы уже жаловались на эту картину.
adt-painting-report-cooldown = Подождите немного перед следующей жалобой.
adt-painting-report-admin = жалуется на картину «{$title}» ({$uid}): {$reason}. Жалобы - во вкладке «Жалобы» админ-панели.

adt-painting-reports-tab = Жалобы
adt-painting-reports-filter-open = Открытые
adt-painting-reports-filter-resolved = Закрытые
adt-painting-reports-resolve = Закрыть жалобу
adt-painting-reports-hide = Скрыть картину
adt-painting-reports-entry = {$title} · {$reporter} · {$date} · открытых: {$count}
adt-painting-reports-empty = Выберите жалобу в списке.
adt-painting-reports-not-found = Жалоба не найдена или уже закрыта.
adt-painting-reports-resolved = Жалоба закрыта.
adt-painting-reports-details =
    {$title} · {$status}
    Автор картины: {$owner}{$banned}
    Пожаловался: {$reporter}, {$date}, раунд {$round}
    Причина: {$reason}
    Открытых жалоб на картину: {$count}
    Закрыл: {$resolver}, {$resolved}
admin-player-actions-window-painting-queue = Очередь картин
