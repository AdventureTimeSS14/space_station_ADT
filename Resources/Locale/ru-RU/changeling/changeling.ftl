# antag selection
roles-antag-changeling-name = Генокрад
roles-antag-changeling-objective = Умный хищник, приобретающий личности своих жертв.

# devour
changeling-devour-attempt-failed-cannot-devour = Мы не можем поглотить это!
changeling-devour-attempt-failed-already-devoured = Мы уже поглотили это тело!
changeling-devour-attempt-failed-devoured-recently = Это тело слишком изуродовано, чтобы его поглотить!
changeling-devour-attempt-failed-not-dead = Это тело ещё живо! Мы не можем поглотить его живым!
changeling-devour-attempt-failed-rotting = У этого трупа только гниющая биомасса.
changeling-devour-attempt-failed-protected = Биомасса этой жертвы защищена бронёй!
changeling-devour-attempt-failed-no-space = Мы превысили максимальное число личин!

changeling-devour-begin-windup-self = Наша жуткая пасть раскрывается с неземным голодом.
changeling-devour-begin-windup-others = { CAPITALIZE(POSS-ADJ($user)) } жуткая пасть раскрывается с неземным голодом.
changeling-devour-begin-consume-self = Наша жуткая пасть глубоко впивается в жертву.
changeling-devour-begin-consume-others = { CAPITALIZE(POSS-ADJ($user)) } жуткая пасть глубоко впивается в жертву.
changeling-devour-consume-complete-self = Наша жуткая пасть скрывается, биомасса поглощена.
changeling-devour-consume-complete-others = { CAPITALIZE(POSS-ADJ($user)) } жуткая пасть прячется.
changeling-devour-doafter-windup = [color=red]{ CAPITALIZE(POSS-ADJ($user)) } лицо раскрывается, готовясь к пиршеству![/color]
changeling-devour-doafter = [color=red]{ CAPITALIZE(SUBJECT($user)) } вгрызается в плоть { $target }![/color]

# transformation
changeling-transform-attempt-self = Наши кости хрустят, мышцы рвутся, одна плоть превращается в другую.
changeling-transform-attempt-others = { CAPITALIZE(POSS-ADJ($user)) } кости хрустят, мышцы рвутся, тело превращается в другое.

changeling-transform-doafter = [color=red]{ CAPITALIZE(POSS-ADJ($user)) } кожа плавится, принимая иную форму.[/color]

# transformation BUI
changeling-transform-bui-select-entity = { $entity }
changeling-transform-bui-drop-identity-menu = Стереть поглощённую личность из памяти.
changeling-transform-bui-drop-identity-entity = Стереть { $entity }
changeling-transform-bui-drop-identity-entity-popup = Мы стёрли { $entity } из нашей памяти.
changeling-transform-bui-drop-identity-cannot-drop = Мы не можем стереть нашу текущую личность.

# voice mimicry
changeling-voice-mimic-window-title = Имитация голоса

# flesh clothing
changeling-flesh-clothing-removed-popop = { CAPITALIZE(THE($item)) } распадается на куски плоти!
changeling-flesh-clothing-examine-wearer = [color=crimson]Этот предмет - замаскированная часть вашего тела. Он исчезнет, если вы его снимете![/color]
changeling-flesh-clothing-alert-name = Одежда из плоти
changeling-flesh-clothing-alert-desc = Включено ли превращение одежды. Нажмите, чтобы переключить.

# biodegrade
changeling-biodegrade-used-popup = { CAPITALIZE(THE($user)) } извергает кислоту на { $restraint }!
changeling-biodegrade-used-popup-self = Мы извергаем кислоту на { $restraint }!

# last resort
changeling-takeover-not-dead = Это тело ещё живо! Мы не можем заразить его живым.
changeling-takeover-is-changeling = Это тело отторгает наши попытки захвата!
changeling-takeover-start-others = { CAPITALIZE(THE($user)) } начинает заползать внутрь трупа!
changeling-takeover-doafter = [color=red]{ CAPITALIZE(SUBJECT($user)) } заползает внутрь { $target }![/color]

# stings
changeling-sting-success = Мы незаметно жалим { THE($target) }!
changeling-sting-hemotoxin = Вы чувствуете резкую боль в груди!

# mindshield
changeling-fake-mindshield-enabled = Мы излучаем поддельные волны щита разума.
changeling-fake-mindshield-disabled = Мы перестаём излучать поддельные волны щита разума.

# chemicals
changeling-not-enough-chemicals = У нас недостаточно химикатов.

# other
changeling-action-fail-generic = Сейчас мы не можем этого сделать.
changeling-cocoon-success = Мы заключаем { THE($target) } в гробницу из плоти.
changeling-cocoon-doafter = [color=red]{ CAPITALIZE(SUBJECT($user)) } покрывает { THE($target) } массой плоти![/color]

# horror transformation
changeling-horror-alert-time-name = Оставшееся время
changeling-horror-alert-time-desc = Когда счётчик дойдёт до нуля, наши силы иссякнут.
changeling-horror-force-transform-self = Наша энергия иссякла, мы больше не можем удерживать эту форму.
changeling-horror-force-transform-others = { CAPITALIZE(POSS-ADJ($user)) } плоть стремительно ссыхается и отмирает.
changeling-horror-transform-fail = У нас недостаточно ДНК, чтобы раскрыть себя.

changeling-paused-map-name = Карта хранения личностей генокрада
