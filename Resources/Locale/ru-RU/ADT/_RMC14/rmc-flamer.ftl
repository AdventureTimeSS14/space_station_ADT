rmc-flamer-empty = Бак пуст!
rmc-flamer-ignite-first = Сначала нужно зажечь запальник!
rmc-flamer-ignite-first-with = Сначала нужно зажечь запальник, нажав { $key }!
rmc-flamer-ignite-action-examine = Уникальное действие переключает запальник.
rmc-flamer-refill = Вы заправляете { $refilled }.
rmc-flamer-tank-not-whitelisted = { CAPITALIZE($tank) } не принимает эту жидкость.
rmc-flamer-tank-not-potent-enough = Эта жидкость недостаточно горюча.
rmc-flamer-tank-examine-intensity = Предельная сила пламени: [color=orange]{ $value }[/color].
rmc-flamer-tank-examine-duration = Предельное время горения: [color=orange]{ $value }[/color] сек.
rmc-flamer-tank-examine-range = Предельная дальность струи: [color=orange]{ $value }[/color] м.

ent-RMCTileFire = огонь
    .desc = Горит.

rmc-fire-pat-self = Вы пытаетесь сбить пламя с { $target }!
rmc-fire-pat-target = { CAPITALIZE($user) } пытается сбить с вас пламя!
rmc-fire-pat-others = { CAPITALIZE($user) } пытается сбить пламя с { $target }!

rmc-immune-to-ignition-examine = { CAPITALIZE($ent) } { $direct ->
        [True] не загорается даже от прямого попадания огнесмеси
       *[other] не загорается от слабого пламени
    }.
rmc-immune-to-fire-tile-damage-examine = [color=#00FFD5]{ CAPITALIZE($ent) } не получает урона от огня.[/color]
rmc-fire-armor-debuff-modifier-examine = { CAPITALIZE($ent) } на { $percentage }% меньше страдает от того, что пламя разъедает броню.

rmc-molotov-can-craft = [color=cyan]Из этого можно сделать молотов, добавив бумагу.[/color]
rmc-molotov-empty = { CAPITALIZE($bottle) } пуста...
rmc-molotov-not-flammable = В { $bottle } слишком мало горючего!

reagent-name-rmcnapalm-ut = UT-напталь
reagent-desc-rmcnapalm-ut = Стандартная огнесмесь для огнемётов. Горит жарко и недолго.
