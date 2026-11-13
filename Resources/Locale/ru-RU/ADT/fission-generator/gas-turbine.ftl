### Examine

gas-turbine-examine-stator-null = Похоже, статор отсутствует.
gas-turbine-examine-stator = Статор на месте.

gas-turbine-examine-blade-null = Похоже, лопасть турбины отсутствует.
gas-turbine-examine-blade = Лопасть турбины на месте.

gas-turbine-spinning-0 = Лопасти не вращаются.
gas-turbine-spinning-1 = Лопасти медленно поворачиваются.
gas-turbine-spinning-2 = Лопасти вращаются.
gas-turbine-spinning-3 = Лопасти вращаются быстро.
gas-turbine-spinning-4 = [color=red]Лопасти вращаются неуправляемо![/color]

gas-turbine-damaged-0 = Выглядит исправной.[/color]
gas-turbine-damaged-1 = Турбина выглядит слегка поцарапанной.[/color]
gas-turbine-damaged-2 = [color=yellow]Турбина выглядит сильно поврежденной.[/color]
gas-turbine-damaged-3 = [color=orange]Она критически повреждена![/color]

gas-turbine-ruined = [color=red]Она полностью сломана![/color]

### Popups

# Shown when an event occurs
gas-turbine-overheat = {$owner} срабатывает аварийный клапан сброса перегрева!
gas-turbine-explode = {CAPITALIZE(THE($owner))} разрывает саму себя!

# Shown when damage occurs
gas-turbine-spark = {CAPITALIZE(THE($owner))} начинает искрить!
gas-turbine-spark-stop = {CAPITALIZE(THE($owner))} перестает искрить.
gas-turbine-smoke = {CAPITALIZE(THE($owner))} начинает дымиться!
gas-turbine-smoke-stop = {CAPITALIZE(THE($owner))} перестает дымиться.

# Shown during repairs
gas-turbine-repair-fail-blade = Сначала нужно заменить лопасть турбины, иначе ремонт невозможен.
gas-turbine-repair-fail-stator = Сначала нужно заменить статор, иначе ремонт невозможен.
gas-turbine-repair-ruined = Вы чините корпус {THE($target)} с помощью {THE($tool)}.
gas-turbine-repair-partial = Вы частично чините повреждения {THE($target)} с помощью {THE($tool)}.
gas-turbine-repair-complete = Вы заканчиваете ремонт {THE($target)} с помощью {THE($tool)}.
gas-turbine-repair-no-damage = На {THE($target)} нет повреждений для ремонта с помощью {THE($tool)}.

# Anchoring warnings
gas-turbine-unanchor-warning = Нельзя открепить {THE($owner)}, пока турбина вращается!
gas-turbine-anchor-warning = Неподходящее место для крепления.

gas-turbine-eject-fail-speed = Нельзя извлекать детали турбины, пока она вращается!
gas-turbine-insert-fail-speed = Нельзя вставлять детали турбины, пока она вращается!

### UI

# Shown when using the UI
gas-turbine-ui-title = Газовая турбина
gas-turbine-ui-tab-main = Управление
gas-turbine-ui-tab-parts = Детали

gas-turbine-ui-rpm = Обороты

gas-turbine-ui-overspeed = ПРЕВЫШЕНИЕ
gas-turbine-ui-overtemp = ПЕРЕГРЕВ
gas-turbine-ui-stalling = СТОП
gas-turbine-ui-undertemp = НЕДОГРЕВ

gas-turbine-ui-flow-rate = Поток
gas-turbine-ui-stator-load = Нагрузка статора

gas-turbine-ui-blade = Лопасть турбины
gas-turbine-ui-blade-integrity = Целостность
gas-turbine-ui-blade-stress = Нагрузка

gas-turbine-ui-stator = Статор турбины
gas-turbine-ui-stator-potential = Потенциал
gas-turbine-ui-stator-supply = Выдача

gas-turbine-ui-power = { POWERWATTS($power) }

gas-turbine-ui-locked-message = Управление заблокировано.
gas-turbine-ui-footer-left = Опасно: быстро движущиеся механизмы.
gas-turbine-ui-footer-right = 2.1 REV 1
