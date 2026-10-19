ent-ActionDissociativeManifest = Проявиться
    .desc = Выйти из тела хозяина или вернуться обратно. Снаружи вас видит только хозяин.
ent-ActionDissociativeFront = Перехватить контроль
    .desc = Забрать тело хозяина себе на время.
ent-ActionDissociativeRelease = Вернуть контроль
    .desc = Отдать тело хозяину раньше времени.
ent-ActionDissociativeSetup = Настроить личность
    .desc = Выбрать имя, пол, голос и внешность. Доступно один раз.

dissociative-not-in-control = Сейчас телом управляете не вы.
dissociative-manifest-host-contained = Хозяин заперт, выйти не получится.
dissociative-front-cooldown = Вы ещё не набрались сил. Осталось примерно { $minutes } мин.
dissociative-front-suppressed = Таблетки глушат вас, пробиться не выходит.
dissociative-front-mindshield = Что-то в голове хозяина не пускает вас наружу.
dissociative-front-host-incapacitated = Тело хозяина сейчас недееспособно.
dissociative-front-start-alter = Тело ваше.
dissociative-front-start-host = Вы теряете контроль над телом...
dissociative-front-end-host = Вы снова управляете собой. Последнее время как в тумане.
dissociative-front-end-alter = Вас затягивает обратно.
dissociative-examine-fronting = [color=#b81500]Взгляд какой-то чужой.[/color]
dissociative-setup-done = Личность сформирована.

dissociative-setup-window-title = Вторая личность
dissociative-setup-window-info = Имя и пол видит только хозяин. Настроить можно один раз.
dissociative-setup-window-name = Имя
dissociative-setup-window-sex = Пол
dissociative-setup-window-customize = Внешность и голос
dissociative-setup-window-confirm = Подтвердить
dissociative-setup-sex-male = Мужской
dissociative-setup-sex-female = Женский
dissociative-setup-sex-unsexed = Нет

ghost-role-information-dissociative-alter-name = Вторая личность ({ $host })
ghost-role-information-dissociative-alter-description = Вы вторая личность, живущая в чужой голове. Говорите с хозяином, выходите наружу и иногда забирайте тело.
ghost-role-information-dissociative-alter-rules =
    Вы [color={ role-type-familiar-color }][bold]вторая личность[/bold][/color] персонажа. Вас видит и слышит только хозяин.
    Вы не антагонист, если вам не выдали роль антагониста. Захватив тело, вы подчиняетесь тем же правилам, что и любой член экипажа.
    Вы не помните ничего из своей предыдущей жизни и не помните ничего из того, что узнали, будучи призраком.

reagent-effect-guidebook-antipsychotic =
    { $chance ->
        [1] Подавляет
       *[other] подавляют
    } вторую личность

ent-ActionDissociativeSurrender = Перестать бороться
    .desc = Навсегда отдать тело второй личности и самому стать голосом внутри.
dissociative-surrender-confirm = Нажмите ещё раз, чтобы навсегда отдать тело.
dissociative-surrender-no-alter = Внутри никого нет, сдаваться некому.
dissociative-surrender-alter = Хозяин сдался. Тело теперь ваше навсегда.
dissociative-surrender-host = Вы перестаёте сопротивляться и растворяетесь внутри.

ent-ActionMidroundCustomization = Изменить внешность
    .desc = Изменить причёску, растительность на лице и голос.

ent-ActionDissociativeGiveControl = Отдать тело
    .desc = Добровольно отдать тело второй личности на то же время, что и при её перехвате.
dissociative-give-control-confirm = Нажмите ещё раз, чтобы отдать тело второй личности.
dissociative-give-control-no-alter = Вторая личность сейчас не может принять тело.

reagent-effect-guidebook-dissociative-extend =
    { $chance ->
        [1] Продлевает
       *[other] продлевают
    } пребывание второй личности в теле на { $seconds } с
reagent-effect-guidebook-dissociative-force =
    { $chance ->
        [1] Выпускает
       *[other] выпускают
    } вторую личность наружу
reagent-effect-guidebook-dissociative-cooldown =
    { $chance ->
        [1] Ускоряет
       *[other] ускоряют
    } перезарядку второй личности
