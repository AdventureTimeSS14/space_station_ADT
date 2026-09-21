objectives-round-end-result = [color=#c4a747][bold]{ CAPITALIZE($agent) }[/bold][/color] - { $count } { $count ->
        [one] игрок
        [few] игрока
       *[other] игроков
    }
objectives-round-end-result-in-custody = [color=gray]Арестовано:[/color] { $custody } из { $count }.
objectives-player-user-named = [color=White]{ $name }[/color] ([color=gray]{ $user }[/color])
objectives-player-named = [color=White]{ $name }[/color]
objectives-no-objectives = { $custody }{ $title } - без целей.
objectives-with-objectives = { $custody }{ $title } - цели:
objectives-listed-under = { $custody }{ $title } - цели указаны выше, в разделе "{ $agent }".
objectives-objective-success = { $objective } | [color=green]Успех![/color] ({ TOSTRING($progress, "P0") })
objectives-objective-partial-success = { $objective } | [color=yellow]Частичный успех![/color] ({ TOSTRING($progress, "P0") })
objectives-objective-partial-failure = { $objective } | [color=orange]Частичный провал![/color] ({ TOSTRING($progress, "P0") })
objectives-objective-fail = { $objective } | [color=red]Провал![/color] ({ TOSTRING($progress, "P0") })
objectives-in-custody = [bold][color=red]| АРЕСТОВАН | [/color][/bold]
