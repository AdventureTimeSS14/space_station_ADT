markings-used = Используемые черты
markings-unused = Неиспользуемые черты
markings-add = Добавить черту
markings-remove = Убрать черту
markings-rank-up = Вверх
markings-rank-down = Вниз
markings-search = Поиск
marking-points-remaining = Черт осталось: { $points }
marking-used = { $marking-name }
marking-used-forced = { $marking-name } (Принудительно)
marking-slot-add = Добавить
marking-slot-remove = Удалить
marking-slot = Слот { $number }
humanoid-marking-modifier-force = Принудительно
humanoid-marking-modifier-ignore-species = Игнорировать вид
humanoid-marking-modifier-base-layers = Базовый слой
humanoid-marking-modifier-enable = Включить
humanoid-marking-modifier-prototype-id = ID прототипа:
humanoid-marking-modifier-respect-limits = Учитывать ограничения
humanoid-marking-modifier-respect-group-sex = Учитывать вид и пол

-markings-selection = { $selectable ->
    [0] Больше нельзя выбрать.
    [one] Можно выбрать ещё одну.
   *[other] Можно выбрать ещё { $selectable }.
}
markings-limits = { $required ->
    [true] { $count ->
        [-1] Выберите хотя бы одну черту.
        [0] Нельзя выбрать черты, но обязательно нужно? Это баг.
        [one] Выберите одну черту.
       *[other] Выберите хотя бы одну черту, не более {$count}. { -markings-selection(selectable: $selectable) }
    }
   *[false] { $count ->
        [-1] Можно выбрать любое количество черт.
        [0] Нельзя выбрать черты.
        [one] Выберите не более одной черты.
       *[other] Выберите не более {$count} черт. { -markings-selection(selectable: $selectable) }
    }
}
markings-reorder = Изменить порядок

# Organ categories (new nubody system)

markings-organ-Torso = Туловище
markings-organ-Head = Голова
markings-organ-ArmLeft = Левая рука
markings-organ-ArmRight = Правая рука
markings-organ-HandRight = Правая кисть
markings-organ-HandLeft = Левая кисть
markings-organ-LegLeft = Левая нога
markings-organ-LegRight = Правая нога
markings-organ-FootLeft = Левая стопа
markings-organ-FootRight = Правая стопа
markings-organ-Eyes = Глаза

# Layer names (new nubody system)

markings-layer-Special = Специальное
markings-layer-Tail = Хвост
markings-layer-Tail-Moth = Крылья
markings-layer-Hair = Причёска
markings-layer-FacialHair = Лицевая растительность
markings-layer-UndergarmentTop = Нижнее бельё (верх)
markings-layer-UndergarmentBottom = Нижнее бельё (низ)
markings-layer-Chest = Грудь
markings-layer-Head = Голова
markings-layer-Snout = Морда
markings-layer-SnoutCover = Морда (покрытие)
markings-layer-HeadSide = Голова (бок)
markings-layer-HeadTop = Голова (верх)
markings-layer-Eyes = Глаза
markings-layer-RArm = Правая рука
markings-layer-LArm = Левая рука
markings-layer-RHand = Правая кисть
markings-layer-LHand = Левая кисть
markings-layer-RLeg = Правая нога
markings-layer-LLeg = Левая нога
markings-layer-RFoot = Правая стопа
markings-layer-LFoot = Левая стопа
markings-layer-Overlay = Наложение
markings-layer-TailOverlay = Наложение (хвост)
