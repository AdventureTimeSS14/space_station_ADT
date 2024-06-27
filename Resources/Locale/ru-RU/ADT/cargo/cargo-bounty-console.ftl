bounty-console-menu-title = Консоль поручений карго
bounty-console-label-button-text = Распечатать
bounty-console-time-label = Время: [color=orange]{$time}[/color]
bounty-console-reward-label = Награда: [color=limegreen]${$reward}[/color]
bounty-console-manifest-label = Список: [color=gray]{$item}[/color]
bounty-console-manifest-entry =
    { $amount ->
        [1] {$item}
        *[other] {$item} x{$amount}
    }
bounty-console-description-label = [color=gray]{$description}[/color]
bounty-console-id-label = ID#{$id}

bounty-console-flavor-left = Награды получаются от местных заказчиков.
bounty-console-flavor-right = v1.4

bounty-manifest-header = Официальный список поручений карго (ID#{$id})
bounty-manifest-list-start = Список предметов:

ent-ComputerCargoBounty = консоль поручений карго
    .desc = Используется для просмотра активных поручений.
    .suffix = { "" }
