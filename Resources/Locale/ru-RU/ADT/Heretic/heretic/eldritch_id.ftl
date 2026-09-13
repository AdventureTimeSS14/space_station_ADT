eldritch-id-card-component-examine-inverted = Текущий эффект [color=yellow]инвертирован[/color]

eldritch-id-card-component-examine-message =
    Зачаровано Мансусом!
    Использование ID на этой карте или этой карты на другой ID поглотит её и скопирует доступы.
    Использование на паре дверей связывает их: вход в одну переносит в другую, а неверных — на случайный шлюз.
    Альт-клик по ID включает инвертированные порталы: вас — на случайный шлюз, неверных — к месту назначения.

eldritch-id-card-component-on-invert =
    { $inverted ->
      [true] теперь
      *[false] больше не
    } создаёт инвертированные разломы

eldritch-id-card-component-portal-inverted =
    портал { $inverted ->
             [true] инвертирован
             *[false] больше не инвертирован
           }

eldritch-id-card-component-invert = Инвертировать
eldritch-id-card-component-invert-message = Создавать инвертированные порталы.

eldritch-id-card-component-link-one = связь 1/2
eldritch-id-card-component-link-two = связь 2/2

lock-portal-component-clear-portals = Очистить обе связи

lock-portal-component-examine-inverted = [color=yellow]инвертирован[/color]
lock-portal-component-examine-not-inverted = [color=yellow]не инвертирован[/color]

lock-portal-component-examine-message =
    Портал {$status}.
    Клик потусторонней картой инвертирует его.
    Альт-клик потусторонней картой удаляет обе связи.
