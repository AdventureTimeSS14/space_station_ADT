ent-ADTMechArchimedes = Архимед
    .desc = Научно-инженерный мех. Герметичная кабина и джет-режим позволяют работать в открытом космосе как на мини-шаттле. Для боя не предназначен.
ent-ADTMechArchimedesBattery = { ent-ADTMechArchimedes }
    .suffix = Батарея
    .desc = { ent-ADTMechArchimedes.desc }

ent-ADTArchimedesCentralElectronics = центральный модуль управления Архимеда
    .desc = Центр управления электрооборудованием меха Архимед.
ent-ADTArchimedesPeripheralsElectronics = модуль управления периферией Архимеда
    .desc = Система управления электрическими периферийными устройствами меха Архимед.

ent-ADTArchimedesHarness = каркас Архимеда
    .desc = Ядро меха Архимед.
ent-ADTArchimedesLArm = левая рука Архимеда
    .desc = Левая рука меха Архимед. Устанавливается на шасси меха.
ent-ADTArchimedesLLeg = левая нога Архимеда
    .desc = Левая нога меха Архимед. Устанавливается на шасси меха.
ent-ADTArchimedesRLeg = правая нога Архимеда
    .desc = Правая нога меха Архимед. Устанавливается на шасси меха.
ent-ADTArchimedesRArm = правая рука Архимеда
    .desc = Правая рука меха Архимед. Устанавливается на шасси меха.

ent-ADTMechEquipmentToolWelder = навесная сварка Архимеда
    .desc = Сварочный аппарат инструментальной руки Архимеда.
ent-ADTMechEquipmentToolWirecutter = навесные кусачки Архимеда
    .desc = Кусачки инструментальной руки Архимеда.
ent-ADTMechEquipmentToolScrewdriver = навесная отвёртка Архимеда
    .desc = Отвёртка инструментальной руки Архимеда.
ent-ADTMechEquipmentToolWrench = навесной гаечный ключ Архимеда
    .desc = Гаечный ключ инструментальной руки Архимеда.
ent-ADTMechEquipmentToolCrowbar = навесная монтировка Архимеда
    .desc = Монтировка инструментальной руки Архимеда.
ent-ADTMechEquipmentToolWelderExperimental = навесная экспериментальная сварка Архимеда
    .desc = Модернизированная сварка инструментальной руки Архимеда. Работает значительно быстрее.
ent-ADTMechEquipmentToolJawsOfLife = навесные «Челюсти жизни» Архимеда
    .desc = Модернизация, объединяющая кусачки и монтировку. Вскрывает даже запитанные шлюзы.
ent-ADTMechEquipmentToolPowerDrill = навесной шуруповёрт Архимеда
    .desc = Модернизация, объединяющая отвёртку и гаечный ключ. Работает значительно быстрее.

ent-ADTMechEquipmentCockpit = дополнительный кокпит меха
    .desc = Второе место, вваренное в каркас меха. На станции пассажир может только копаться в модулях, а в полёте берёт на себя руки и вооружение.

ent-ADTActionMechThruster = Джет-режим
    .desc = Переключить полётный режим. В полёте расходуется топливо.

ent-ADTActionMechCockpitEject = Покинуть кокпит
    .desc = Выбраться с места второго пилота.

adt-mech-tool-no-energy = Не хватает заряда для работы инструментом!
adt-mech-equipment-slot-occupied = Свободного места под такой модуль больше нет.
adt-mech-arm-slot-occupied = Сменная рука уже занята другим модулем.
adt-mech-cockpit-slot-occupied = Второй кокпит поставить некуда.
adt-mech-cockpit-verb-enter = Занять место второго пилота
adt-mech-cockpit-verb-exit = Покинуть место второго пилота

adt-mech-thruster-on = Джет активирован.
adt-mech-thruster-off = Джет отключён.
adt-mech-thruster-no-fuel = В баке джета нет топлива!
adt-mech-thruster-fuel-full = Бак джета уже полон.
adt-mech-thruster-refuel-active = Нельзя заправлять джет, пока он активен!
adt-mech-thruster-refueled = Джет заправлен: { $fuel }/{ $max }.
adt-mech-thruster-examine = Топливо джета: [color=orange]{ $fuel }/{ $max }[/color]. Заправляется листами плазмы снаружи.
adt-mech-thruster-only-space = Джет можно включить только в невесомости!
