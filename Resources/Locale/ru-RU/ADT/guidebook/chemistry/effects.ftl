reagent-effect-guidebook-embalm =
    { $chance ->
        [1] Предотвращает
       *[other] предотвращают
    } гниение трупов
reagent-effect-guidebook-teleport =
    { $chance ->
        [1] Вызвать
       *[other] вызвать
    } неконтролируемое перемещение
reagent-effect-guidebook-purge-allergies =
    { $chance ->
        [1] Лечит
       *[other] лечит
    } аллергию
reagent-effect-guidebook-adjust-allergic-stack =
    { $chance ->
        [1]
            { $positive ->
                [true] Ослабляет
               *[false] Ухудшает
            }
       *[other]
            { $positive ->
                [true] ослабляет
               *[false] ухудшает
            }
    } симптомы аллергии на { $positive ->
        [true] [color=green]{ $amount }[/color]
       *[false] [color=red]{ $amount }[/color]
    } ед.
reagent-effect-guidebook-wash-stamp-reaction =
    { $chance ->
        [1] Смывает
       *[other] смывают
    } печати с лица
