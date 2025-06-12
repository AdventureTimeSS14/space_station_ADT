### UI

# Shown when a stack is examined in details range
comp-stack-examine-detail-count =
    В стопке [color={ $markupCountColor }]{ $count }[/color] { $count ->
        [one] предмет
        [few] предмета
       *[other] предметов
    }.
# Stack status control
comp-stack-status = Количество: [color=white]{ $count }[/color]

### Interaction Messages

# Shown when attempting to add to a stack that is full
comp-stack-already-full = Стопка уже заполнена.
# Shown when a stack becomes full
comp-stack-becomes-full = Стопка теперь заполнена.
# Text related to splitting a stack
comp-stack-split = Вы разделили стопку.
comp-stack-split-halve = Разделить пополам
comp-stack-split-too-small = Стопка слишком мала для разделения.

# ADT-Tweak start Разделение предметов
comp-stack-split-own-count-verb = Выбрать свое количество
comp-stack-split-choice-label = Выбрать собственное количество
comp-stack-split-choice-field = Количество:
# ADT Tweak end
