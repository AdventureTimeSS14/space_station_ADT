## UI

cargo-console-menu-title = Консоль заказа грузов
cargo-console-menu-account-name-label = Имя аккаунта:{ " " }
cargo-console-menu-account-name-none-text = Нет
cargo-console-menu-shuttle-name-label = Название шаттла:{ " " }
cargo-console-menu-shuttle-name-none-text = Нет
cargo-console-menu-points-label = Кредиты:{ " " }
cargo-console-menu-points-amount = ${ $amount }
cargo-console-menu-shuttle-status-label = Статус шаттла:{ " " }
cargo-console-menu-shuttle-status-away-text = Отбыл
cargo-console-menu-order-capacity-label = Объём заказов:{ " " }
cargo-console-menu-call-shuttle-button = Активировать телепад
cargo-console-menu-permissions-button = Доступы
cargo-console-menu-categories-label = Категории:{ " " }
cargo-console-menu-search-bar-placeholder = Поиск
cargo-console-menu-requests-label = Запросы
cargo-console-menu-orders-label = Заказы
cargo-console-menu-order-reason-description = Причина: { $reason }
cargo-console-menu-populate-categories-all-text = Все
cargo-console-menu-populate-orders-cargo-order-row-product-name-text = { $productName } (x{ $orderAmount }) от { $orderRequester }
cargo-console-menu-cargo-order-row-approve-button = Одобрить
cargo-console-menu-cargo-order-row-cancel-button = Отменить
cargo-console-menu-tab-title-orders = Заказы
cargo-console-menu-tab-title-funds = Переводы
cargo-console-menu-account-action-transfer-limit = [bold]Лимит переводов:[/bold] $ {$limit}
cargo-console-menu-account-action-transfer-limit-unlimited-notifier = [color=red](Без лимита)[/color]
cargo-console-menu-account-action-select = [bold]Действие аккаунта:[/bold]
cargo-console-menu-account-action-amount = [bold]Количество:[/bold] $
cargo-console-menu-account-action-button = Использовать
cargo-console-menu-toggle-account-lock-button = Переключение лимита перевода
cargo-console-menu-account-action-option-withdraw = Снять деньги
cargo-console-menu-account-action-option-transfer = Перевести деньги в {$code}

# Orders
cargo-console-order-not-allowed = Доступ запрещён
cargo-console-station-not-found = Нет доступной станции
cargo-console-invalid-product = Неверный ID продукта
cargo-console-too-many = Слишком много одобренных заказов
cargo-console-snip-snip = Заказ урезан до вместимости
cargo-console-insufficient-funds = Недостаточно средств (требуется { $cost })
cargo-console-unfulfilled = Нет места для выполнения заказа
cargo-console-trade-station = Отправлено на { $destination }
cargo-console-unlock-approved-order-broadcast = [bold]Заказ на { $productName } x{ $orderAmount }[/bold], стоимостью [bold]{ $cost }[/bold], был одобрен [bold]{ $approver }[/bold]
cargo-console-paper-print-name = Заказ #{ $orderNumber }
cargo-console-paper-print-text =
    Заказ #{ $orderNumber }
    Товар: { $itemName }
    Кол-во: { $orderQuantity }
    Запросил: { $requester }
    Причина: { $reason }
    Одобрил: { $approver }
# Cargo shuttle console
cargo-shuttle-console-menu-title = Консоль вызова грузового шаттла
cargo-shuttle-console-station-unknown = Неизвестно
cargo-shuttle-console-shuttle-not-found = Не найден
cargo-no-shuttle = Грузовой шаттл не найден!
cargo-shuttle-console-organics = На шаттле обнаружены органические формы жизни
