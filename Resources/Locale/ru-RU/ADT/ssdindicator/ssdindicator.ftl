comp-ssd-person-examined = [color=yellow]{ CAPITALIZE(SUBJECT($ent)) } { GENDER($ent) ->
    [male] спит
    [female] спит
    [neuter] спит
   *[other] спят
} { $time ->
    [one] {$time} минуту
    [few] {$time} минуты
    [many] {$time} минут
   *[other] {$time} минут
}.[/color]
