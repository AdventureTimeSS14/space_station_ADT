### Интерфейс

rpd-component-examine-mode-details = Выбран режим: '{ $mode }'.
rpd-component-examine-build-details = Выбран режим строительства: { $name }.
rpd-component-examine-secondary-details = Вторичная конфигурация: { $name }. Ставится по Alt+ЛКМ.
### Interaction Messages

# Смена мода
rpd-component-change-mode = РРТ переключён в режим '{ $mode }'.
rpd-component-change-build-mode = РРТ переключён в режим строительства. Строится { $name }.
# Вторичная конфигурация (Alt+ЛКМ)
rpd-component-select-secondary = Выбрать вторичную трубу
rpd-component-change-secondary-mode = Вторичная конфигурация РРТ: '{ $mode }'.
rpd-component-change-secondary-build-mode = Вторичная конфигурация РРТ. Строится { $name }.
# Кол-во материи
rpd-component-no-ammo-message = В РРТ закончились заряды!
rpd-component-insufficient-ammo-message = В РРТ не хватает зарядов!
# Разборка
rpd-component-deconstruct-target-not-on-whitelist-message = Вы не можете демонтировать это!
rpd-component-nothing-to-deconstruct-message = Здесь нечего демонтировать!
# Строительство
rpd-component-cannot-build-on-empty-tile-message = Это не может быть построено без фундамента.
rpd-component-must-build-on-subfloor-message = Это может быть построено только на покрытии!
rpd-component-cannot-build-on-occupied-tile-message = Здесь нельзя строить, место уже занято!

### Имя категориий

rpd-component-DisposalPipe = Утилизационные трубы
rpd-component-Gaspipes = Газовые трубы
rpd-component-Devices = Девайсы

### Дополнительная информация

rpd-component-deconstruct = Демонтаж
rpd-ammo-component-on-examine =
    Содержит { $charges } { $charges ->
        [one] заряд
        [few] заряда
       *[other] зарядов
    }.
rpd-ammo-component-after-interact-full = РРТ полон!
rpd-ammo-component-after-interact-refilled = Вы пополняете РРТ.
rpd-component-no-secondary-selected = Вторичная конфигурация не выбрана! Используйте Alt+ЛКМ по РРТ для выбора.
