### Popups
reactor-smoke-start = {CAPITALIZE(THE($owner))} начинает дымиться!
reactor-smoke-stop = {CAPITALIZE(THE($owner))} перестает дымиться.
reactor-fire-start = {CAPITALIZE(THE($owner))} загорается!
reactor-fire-stop = {CAPITALIZE(THE($owner))} перестает гореть.

reactor-unanchor-melted = Нельзя открепить {THE($owner)}, он вплавился в корпус!
reactor-unanchor-warning = Нельзя открепить {THE($owner)}, пока он не пуст или горячее 80C!
reactor-anchor-warning = Неподходящее место для крепления.

### Messages
reactor-smoke-start-message = ВНИМАНИЕ: {CAPITALIZE(THE($owner))} достиг опасной температуры: {$temperature}K. Немедленно вмешайтесь, чтобы предотвратить расплавление.
reactor-smoke-stop-message = {CAPITALIZE(THE($owner))} остыл ниже опасной температуры. Хорошего дня.
reactor-fire-start-message = ВНИМАНИЕ: {CAPITALIZE(THE($owner))} достиг КРИТИЧЕСКОЙ температуры: {$temperature}K. РАСПЛАВЛЕНИЕ НЕИЗБЕЖНО.
reactor-fire-stop-message = {CAPITALIZE(THE($owner))} остыл ниже критической температуры. Расплавление предотвращено.

reactor-temperature-dangerous-message = {CAPITALIZE(THE($owner))} имеет опасную температуру: {$temperature}K.
reactor-temperature-critical-message = {CAPITALIZE(THE($owner))} имеет критическую температуру: {$temperature}K.
reactor-temperature-cooling-message = {CAPITALIZE(THE($owner))} остывает: {$temperature}K.

reactor-melting-announcement = Ядерный реактор на станции начинает плавиться. Рекомендуется эвакуация окружающей зоны.
reactor-melting-announcement-sender = Ядерная тревога

reactor-meltdown-announcement = Ядерный реактор на станции катастрофически перегружен. Вероятны радиоактивные обломки, осадки и пожары. Настоятельно рекомендуется немедленная эвакуация окружающей зоны.
reactor-meltdown-announcement-sender = Ядерное расплавление

### UI
comp-nuclear-reactor-ui-locked = Заблокировано
comp-nuclear-reactor-ui-insert-button = Вставить
comp-nuclear-reactor-ui-remove-button = Извлечь
comp-nuclear-reactor-ui-eject-button = Выбросить

comp-nuclear-reactor-ui-view-change = Сменить вид
comp-nuclear-reactor-ui-view-temp = Температура
comp-nuclear-reactor-ui-view-neutron = Нейтроны
comp-nuclear-reactor-ui-view-fuel = Топливо

comp-nuclear-reactor-ui-status-panel = Статус реактора
comp-nuclear-reactor-ui-reactor-temp = Температура
comp-nuclear-reactor-ui-reactor-rads = Радиация
comp-nuclear-reactor-ui-reactor-therm = Тепловая мощность
comp-nuclear-reactor-ui-reactor-control = Стержни
comp-nuclear-reactor-ui-therm-format = { POWERWATTS($power) }т

comp-nuclear-reactor-ui-footer-left = Опасно: высокая радиация.
comp-nuclear-reactor-ui-footer-right = 1.0 REV 1
