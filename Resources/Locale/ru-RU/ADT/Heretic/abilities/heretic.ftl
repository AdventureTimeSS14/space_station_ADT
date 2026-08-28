## misc
mansus-infused-item-examine = [color=purple]Одно лишь присутствие этого предмета вызывает у вас дрожь. Стоит ли его поднимать?[/color]

heretic-ability-fail = Не удалось использовать заклинание
heretic-ability-fail-magicitem = Вы не можете произнести заклинание без концентрации!
heretic-ability-fail-notarget = Не удалось найти цель для заклинания!
heretic-magicitem-examine = [color=yellow]Данный предмет позволяет вам использовать продвинутые заклинания, пока экипирован или находится в руках.[/color]
heretic-blade-examine = [color=yellow]После использования в руке, вы сломаете данный клинок и телепортируетесь подальше от опасности.[/color]
heretic-blade-void-examine = [color=yellow]Если на враге есть Метка — вы телепортируетесь к нему. Если Меток нет — вы телепортируетесь в безопасное место.[/color]
heretic-blade-use = Лезвие разлетается на куски, и вы чувствуете, как голоса уводят вас прочь.
heretic-riposte-used = Парирование использовано!
heretic-riposte-available = Парирование доступно!
heretic-rust-mark-itembreak = { $name } разлетается на куски!
heretic-manselink-fail-exists = Это существо уже связано с вами!
heretic-manselink-fail-nomind = У этого существа отсутствует разум!
heretic-manselink-start = Вы начинаете связывать разум существа со своим.
heretic-manselink-start-target = Вы чувствуете, как ваш разум куда-то утягивает...
heretic-livingheart-notargets = Нет доступных целей. Посетите руну.
heretic-livingheart-offstation = { $state } в направлении к { $direction }у!
heretic-livingheart-onstation = { $state } в направелии к { $direction }у!
heretic-livingheart-unknown = Он... не в этой реальности.

# ADT: ent-HereticProtectiveBlade* live in prototypes/entities/objects/specific/heretic.ftl

## speech

heretic-speech-mansusgrasp = R'CH T'H TR'TH!
heretic-speech-ash-jaunt = ASH'N P'SSG'
heretic-speech-ash-volcano = V'LC'N!
heretic-speech-ash-rebirth = G'LR'Y T' TH' N'GHT'W'TCH'ER!
heretic-speech-ash-flame = FL'MS!!
heretic-speech-ash-cascade = C'SC'DE!!
heretic-speech-blade-furioussteel = F'LSH'NG S'LV'R!
heretic-speech-flesh-surgery = CL'M M'N!
heretic-speech-flesh-worm = REALITY UNCOIL!!
heretic-speech-ghoul-call = C'M' T'M M'!!
heretic-ghoul-call-success = { $count } гуль(ей) телепортировано к вам!
heretic-ghoul-call-no-ghouls = У вас нет гулей!
heretic-speech-rust-spread = A'GRSV SPR'D
heretic-speech-rust-plume = 'NTR'P'C PL'M'
heretic-speech-void-blast = F'RZ'E!
heretic-speech-void-phase = RE'L'T' PH'S'E!
heretic-speech-void-pull = BR'NG F'RTH TH'M T' M'!!
heretic-speech-cleave = CL'VE
heretic-speech-bloodsiphon = FL'MS O'ET'RN'ITY
heretic-speech-mansuslink = PI'RC' TH' M'ND
heretic-speech-realignment = R'S'T
heretic-speech-fuckoff = F'K 'FF!!

## technically applied to heretic's spawns only but it stays here because why not.

heretic-speech-blind = E'E'S
heretic-speech-emp = E'P
heretic-speech-shapeshift = SH'PE
heretic-speech-link = PI'RC' TH' M'ND

heretic-cant-shoot = Я не могу использовать {$entity} из-за моей священной приверженности пути клинка.
heretic-ability-fail-lowhealth = Это заклинание наносит {$damage} урона, оно введёт вас в критическое состояние, если его использовать!

# ADT: добавлено при актуализации еретика с Goob
heretic-ability-fail-tile-not-rusted = Выбранное покрытие должно быть ржавым, чтобы использовать эту способность!
heretic-ability-fail-tile-underneath-not-rusted = Плитка, на которой вы стоите, должна быть ржавой, чтобы использовать эту способность!
heretic-ability-fail-tile-occupied = Покрытие занято!
heretic-ability-fail-rust-stage-low = Вы недостаточно сильны чтобы покрыть ржавчиной данное покрытие!
heretic-ability-fail-target-ghoul = Цель уже гуль!
heretic-ability-fail-target-no-mind = У цели нет души!
heretic-ability-lose-focus-shadow-cloak = Когда вы теряете фокус, тени вытягивают вас наружу!
heretic-cosmic-rune-fail-star-mark = Заблокировано звёздной меткой!
heretic-cosmic-rune-fail-unlinked = Руна не присоединена!
heretic-cosmic-rune-fail-range = Недостаточно близко!
mansus-grasp-trigger-fail = Что-то мешает вам активировать это!
heretic-livingheart-faraway = Оно { $state ->
    [dead] мертво
    [alive] живо
    *[other] в неизвестном состоянии
}, очень далеко отсюда!
heretic-stargaze-obliterate-other = Вы видите, как {$uid} охвачен обжигающим гневом космоса. На мгновение вы видите, как их силуэты бьются в агонии, прежде чем рассыпаться на атомы.
heretic-stargaze-obliterate-user = СИЛА САМОГО КОСМОСА ИЗЛИВАЕТСЯ НА ВАС. ВОЛНЫ ЖАРА ОХВАТЫВАЮТ ВАШЕ ТЕЛО, РАЗРЫВАЯ ЕГО ПО ШВАМ. ВАШЕ ПОЛНОЕ УНИЧТОЖЕНИЕ ДЛИТСЯ ВСЕГО МГНОВЕНИЕ, ПРЕЖДЕ ЧЕМ ВЫ СНОВА СТАНЕТЕ ТЕМ, КЕМ БЫЛИ ВСЕГДА. КУСОЧКИ ПРЕВРАЩАЮТСЯ В ПЫЛЬ...
heretic-stargazer-reset-consciousness = ЭТО ДЕЙСТВИЕ НЕОБРАТИМО ИЗМЕНИТ РАЗУМ ЗВЕЗДОЧЁТА! Используйте ещё раз для подтверждения.
heretic-stargazer-consciousness-reset-fail = Похоже, что ваш запрос на изменение разума звездочёта был отклонён... Похоже, что на данный момент вы застряли с этим.
heretic-stargazer-consciousness-reset-target = Ваш призыватель перезагрузил вас, и вашим телом завладел призрак. Похоже, он был недоволен вашим выступлением.
heretic-stargazer-consciousness-reset-user = Разум звездочёта исказился, чтобы лучше подходить вам.
heretic-speech-rust-wave = SPR'D TH' WO'D!
heretic-speech-void-prison = V'D PR'S'N!
heretic-speech-void-conduit = MBR'C' TH' V''D!
heretic-speech-cosmic-rune = ST'R R'N'!
heretic-speech-star-touch = ST'R 'N'RG'!!
heretic-speech-star-blast = R'T'T' ST'R!!
heretic-speech-cosmic-expansion = C'SM'S 'XP'ND!
heretic-speech-stargaze = SH''P D' W''P
heretic-speech-ice-spear = D'WN 'F TH'CE!
heretic-speech-shapeshft = SH'PE
heretic-blade-break-fail-acended-message = Вы не можете сломать клинок после вознесения!

heretic-grasp-fail-invalid-target = Хватке здесь не за что ухватиться.

mansus-grasp-drain = Хватка Мансуса высасывает энергию из цели!
