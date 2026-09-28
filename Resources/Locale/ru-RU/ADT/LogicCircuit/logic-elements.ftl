# Контакты

logic-pin-a = А
logic-pin-b = Б
logic-pin-value = Значение
logic-pin-result = Результат
logic-pin-signal = Сигнал
logic-pin-write = Запись
logic-pin-push = Добавить
logic-pin-reset = Сброс
logic-pin-toggle = Переключить
logic-pin-enable = Включение
logic-pin-rising = Появление
logic-pin-falling = Пропадание

# Настройки

logic-config-value = Значение
logic-config-digits = Знаков после запятой
logic-config-epsilon = Допуск
logic-config-length = Длина
logic-config-delay = Задержка, с
logic-config-frequency = Частота, Гц
logic-config-separator = Разделитель
logic-config-port = Порт
logic-config-replacement = Подставлять значение

# Разделы палитры

logic-category-special = Прочее
logic-category-logic = Логика
logic-category-arithmetic = Арифметика
logic-category-comparison = Сравнение
logic-category-memory = Память и время
logic-category-signal = Устройства
logic-category-text = Текст

# Логика

logic-element-and = И
logic-element-and-desc = Выдаёт сигнал, только если есть оба входных.
logic-element-or = ИЛИ
logic-element-or-desc = Выдаёт сигнал, если есть хотя бы один входной.
logic-element-not = НЕ
logic-element-not-desc = Выдаёт сигнал, когда на входе его нет.
logic-element-xor = ИСКЛЮЧАЮЩЕЕ ИЛИ
logic-element-xor-desc = Выдаёт сигнал, когда входы различаются.

# Арифметика

logic-element-add = Сложение
logic-element-add-desc = Складывает два числа.
logic-element-subtract = Вычитание
logic-element-subtract-desc = Вычитает Б из А.
logic-element-multiply = Умножение
logic-element-multiply-desc = Перемножает два числа.
logic-element-divide = Деление
logic-element-divide-desc = Делит А на Б. Деление на ноль даёт ноль.
logic-element-modulo = Остаток
logic-element-modulo-desc = Остаток от деления А на Б.
logic-element-abs = Модуль
logic-element-abs-desc = Убирает знак числа.
logic-element-round = Округление
logic-element-round-desc = Округляет число до заданного числа знаков.
logic-element-min = Минимум
logic-element-min-desc = Меньшее из двух чисел.
logic-element-max = Максимум
logic-element-max-desc = Большее из двух чисел.

# Сравнение

logic-element-equals = Равно
logic-element-equals-desc = Сравнивает входы. Числа сравниваются с допуском, всё остальное как текст.
logic-element-greater = Больше
logic-element-greater-desc = Выдаёт сигнал, когда А больше Б.
logic-element-less = Меньше
logic-element-less-desc = Выдаёт сигнал, когда А меньше Б.

# Память и время

logic-element-constant = Константа
logic-element-constant-desc = Постоянно выдаёт заданное значение.
logic-element-memory = Память
logic-element-memory-desc = Запоминает значение, пока есть сигнал на входе записи, и выдаёт запомненное.
logic-element-shift-register = Сдвиговый регистр
logic-element-shift-register-desc = Копит значения в строку и держит последние символы. На нём собирается кодовый замок.
logic-element-toggle = Переключатель
logic-element-toggle-desc = Каждое нажатие меняет состояние на противоположное.
logic-element-edge-detector = Детектор фронта
logic-element-edge-detector-desc = Даёт короткий импульс на появление и на пропадание сигнала.
logic-element-delay = Задержка
logic-element-delay-desc = Пропускает значение на выход через заданное время.
logic-element-oscillator = Генератор
logic-element-oscillator-desc = Мигает выходом с заданной частотой. Пустой вход включения считается неподключённым.

# Текст

logic-element-concatenation = Склейка
logic-element-concatenation-desc = Соединяет два значения в одну строку через разделитель.

# Связь с устройствами

logic-element-signal-in = Вход устройства
logic-element-signal-in-desc = Отдаёт в схему то, что коробка приняла по внешнему порту. Если задана подстановка, выдаёт её, пока на порту есть сигнал.
logic-element-signal-out = Выход устройства
logic-element-signal-out-desc = Отправляет значение наружу по внешнему порту коробки.
logic-element-display = Индикатор
logic-element-display-desc = Пропускает значение через себя, чтобы его было видно в редакторе.

logic-pin-angle = Угол
logic-pin-condition = Условие
logic-pin-if-true = Если да
logic-pin-if-false = Если нет
logic-pin-index = Номер

logic-config-min = Минимум
logic-config-max = Максимум
logic-config-table = Записи через |
logic-config-start = Начало
logic-config-count = Сколько
logic-config-channel = Канал

logic-element-sin = Синус
logic-element-sin-desc = Синус угла в градусах.
logic-element-cos = Косинус
logic-element-cos-desc = Косинус угла в градусах.
logic-element-tan = Тангенс
logic-element-tan-desc = Тангенс угла в градусах.
logic-element-asin = Арксинус
logic-element-asin-desc = Угол в градусах по синусу.
logic-element-acos = Арккосинус
logic-element-acos-desc = Угол в градусах по косинусу.
logic-element-atan = Арктангенс
logic-element-atan-desc = Угол в градусах по двум катетам, на всей окружности.
logic-element-pow = Степень
logic-element-pow-desc = Возводит А в степень Б.
logic-element-sqrt = Корень
logic-element-sqrt-desc = Квадратный корень. От отрицательного даёт ноль.
logic-element-log = Логарифм
logic-element-log-desc = Натуральный логарифм. От нуля и отрицательного даёт ноль.
logic-element-exp = Экспонента
logic-element-exp-desc = Возводит число e в заданную степень.
logic-element-floor = Вниз
logic-element-floor-desc = Округляет число вниз.
logic-element-ceil = Вверх
logic-element-ceil-desc = Округляет число вверх.
logic-element-clamp = Ограничение
logic-element-clamp-desc = Зажимает число между минимумом и максимумом.

logic-element-select = Выбор
logic-element-select-desc = Пропускает одно из двух значений в зависимости от условия.
logic-element-lookup = Таблица
logic-element-lookup-desc = По номеру на входе выдаёт одну из записей, перечисленных через вертикальную черту. Номер закольцовывается.
logic-element-substring = Подстрока
logic-element-substring-desc = Кусок значения с заданной позиции и длины.
logic-element-length = Длина
logic-element-length-desc = Сколько символов в значении.

logic-element-wireless-out = Передатчик
logic-element-wireless-out-desc = Кладёт значение на именованный канал. Канал слышен всем коробкам на той же карте.
logic-element-wireless-in = Приёмник
logic-element-wireless-in-desc = Читает значение с именованного канала.
