ore-silo-ui-title = Материальный Склад
ore-silo-ui-label-clients = Устройства
ore-silo-ui-label-mats = Материалы
ore-silo-ui-itemlist-entry = {$linked ->
    [true] {"[Подключено] "}
    *[false] {""}
}{$name} ({$beacon}) {$inRange ->
    [true] {""}
    *[false] (Вне зоны действия)
}