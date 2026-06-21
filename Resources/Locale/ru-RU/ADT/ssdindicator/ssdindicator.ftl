comp-ssd-person-examined = [color=yellow]{ CAPITALIZE(SUBJECT($ent)) } спал уже { $time ->
    [one] { $time } минуту
   *[other] { $time } минут
}.[/color]
