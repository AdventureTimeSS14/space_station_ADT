ent-ADTSpawnPointGhostBlob = спавнер блоба
    .suffix = DEBUG, точка спавна ролей призраков
    .desc = { ent-MarkerBase.desc }
ent-ADTMobBlobPod = капля блоба
    .desc = Обычный боец блоба.
ent-ADTMobBlobBlobbernaut = блобернаут
    .desc = Элитный боец блоба.
ent-ADTBaseBlob = базовый блоб.
    .desc = { "" }
ent-ADTNormalBlobTile = обычный тайл блоба
    .desc = Обычная часть блоба, необходимая для постройки более продвинутых тайлов.
ent-ADTCoreBlobTile = ядро блоба
    .desc = Важнейший орган блоба. При уничтожении ядра заражение прекращается.
ent-ADTFactoryBlobTile = фабрика блоба
    .desc = Со временем производит капли блоба и блобернаутов.
ent-ADTResourceBlobTile = ресурсный блоб
    .desc = Производит ресурсы для блоба.
ent-ADTNodeBlobTile = узел блоба
    .desc = Уменьшенная версия ядра, позволяющая размещать вокруг себя специальные тайлы блоба.
ent-ADTStrongBlobTile = укреплённый тайл блоба
    .desc = Усиленная версия обычного тайла. Не пропускает воздух и защищает от физического урона.
ent-ADTReflectiveBlobTile = отражающий тайл блоба
    .desc = Отражает лазеры, но хуже защищает от физического урона.
    .desc = { "" }

ent-ADTActionCreateBlobFactory = Разместить фабрику блоба (80)
    .desc = Превращает выбранный обычный блоб в фабрику, которая будет производить до 3 спор и блобернаута, если размещена рядом с ядром или узлом.
ent-ADTActionCreateBlobResource = Разместить ресурсный блоб (60)
    .desc = Превращает выбранный обычный блоб в ресурсный, который будет генерировать ресурсы, если размещён рядом с ядром или узлом.
ent-ADTActionCreateBlobNode = Разместить узел блоба (50)
    .desc = Превращает выбранный обычный блоб в узел. Узел активирует эффекты фабрик и ресурсных блобов, лечит другие блобы и медленно разрастается, разрушая стены и создавая обычные блобы.
ent-ADTActionCreateBlobbernaut = Произвести блобернаута (60)
    .desc = Создаёт блобернаута на выбранной фабрике. Каждая фабрика может сделать это только один раз. Блобернаут получает урон за пределами тайлов блоба и лечится рядом с узлами.
ent-ADTActionSplitBlobCore = Расколоть ядро (400)
    .desc = Можно сделать только один раз. Превращает выбранный узел в независимое ядро, которое будет действовать самостоятельно.
ent-ADTActionSwapBlobCore = Перенести ядро (200)
    .desc = Меняет местами расположение вашего ядра и выбранного узла.
ent-ADTActionTeleportBlobToCore = Прыжок к ядру
    .desc = Телепортирует вас к вашему ядру блоба.
ent-ADTActionSwapBlobChem = Сменить химию (70)
    .desc = Позволяет сменить вашу текущую химию.
ent-ADTActionTransformToBlob = Превратиться в блоб
    .desc = Мгновенно уничтожает ваше тело и создаёт ядро блоба. Убедитесь, что стоите на напольном тайле, иначе вы просто исчезнете.
ent-ADTActionDowngradeBlob = Понизить блоб
    .desc = Превращает выбранный тайл обратно в обычный блоб, чтобы установить на его месте другой тип клетки.

objective-issuer-blob = Блоб


ghost-role-information-blobbernaut-name = Блобернаут
ghost-role-information-blobbernaut-description = Вы блобернаут. Вы должны защищать ядро блоба. 

ghost-role-information-blob-name = Блоб
ghost-role-information-blob-description = Вы — блоб. Поглотите станцию.

roles-antag-blob-name = Блоб
roles-antag-blob-objective = Достигните критической массы.
role-subtype-blob = Блоб

guide-entry-blob = Блоб

blob-title = Блоб
blob-description = На станции появился блоб — быстрорастущий паразитический организм, стремящийся поглотить всё живое и превратить станцию в свою массу.

# Всплывающие сообщения
blob-target-normal-blob-invalid = Неверный тип блоба, выберите обычный блоб.
blob-target-factory-blob-invalid = Неверный тип блоба, выберите блоб-фабрику.
blob-target-node-blob-invalid = Неверный тип блоба, выберите блоб-узел.
blob-target-close-to-resource = Слишком близко к другому ресурсному блобу.
blob-target-nearby-not-node = Поблизости нет узла или ресурсного блоба.
blob-target-close-to-node = Слишком близко к другому узлу.
blob-target-already-produce-blobbernaut = Эта фабрика уже произвела блобернаута.
blob-cant-split = Нельзя расколоть ядро блоба.
blob-not-have-nodes = У вас нет узлов.
blob-not-enough-resources = Недостаточно ресурсов.
blob-help = Вам может помочь только бог.
blob-swap-chem = В разработке.
blob-mob-attack-blob = Нельзя атаковать блоб.
blob-get-resource = +{ $point }
blob-spent-resource = -{ $point }
blobberaut-not-on-blob-tile = Вы умираете, находясь не на тайлах блоба.
carrier-blob-alert = У вас осталось { $second } секунд до превращения.
carrier-blob-too-early = Вы ещё не можете превратиться в блоба. Слишком рано.

blob-mob-zombify-second-start = { $pod } начинает превращать вас в зомби.
blob-mob-zombify-third-start = { $pod } начинает превращать { $target } в зомби.

blob-mob-zombify-second-end = { $pod } превращает вас в зомби.
blob-mob-zombify-third-end = { $pod } превращает { $target } в зомби.

blobberaut-factory-destroy = уничтожение фабрики
blob-target-already-connected = уже подключено


# Интерфейс
blob-chem-swap-ui-window-name = Смена химии
blob-chem-reactivespines-info = Реактивные шипы
                                Наносит 25 физического урона.
blob-chem-blazingoil-info = Пылающее масло
                            Наносит 15 урона огнём и поджигает цели.
                            Делает вас уязвимым к воде.
blob-chem-regenerativemateria-info = Регенеративная материя
                                    Наносит 6 физического и 15 токсинового урона.
                                    Ядро блоба регенерирует здоровье в 10 раз быстрее обычного и производит 1 дополнительный ресурс.
blob-chem-explosivelattice-info = Взрывчатая решётка
                                    Наносит 5 урона огнём и взрывает цель, нанося 10 физического урона.
                                    Капли взрываются при смерти.
                                    Вы становитесь невосприимчивы к взрывам.
                                    Вы получаете на 50% больше урона от огня и электрошока.
blob-chem-electromagneticweb-info = Электромагнитная паутина
                                    Наносит 20 урона огнём, 20% шанс вызвать импульс ЭМИ при атаке.
                                    Тайлы блоба создают импульс ЭМИ при разрушении.
                                    Вы получаете на 25% больше физического урона и урона от жара.

blob-alert-out-off-station = Блоб был удалён, так как оказался за пределами станции!

# Объявления
blob-alert-recall-shuttle = Эвакуационный шаттл не может быть отправлен, пока на станции присутствует биологическая угроза 5 уровня.
blob-alert-detect = Подтверждена вспышка биологической угрозы 5 уровня на борту станции. Весь персонал обязан сдержать вспышку.
blob-alert-critical = Уровень биологической угрозы критический, коды ядерной аутентификации отправлены на станцию. Центральное Командование приказывает оставшемуся персоналу привести в действие механизм самоуничтожения.
blob-alert-critical-NoNukeCode = Уровень биологической угрозы критический. Центральное Командование приказывает оставшемуся персоналу укрыться и ожидать спасения.
blob-alert-shuttle-arrived = На борту обнаружена биологическая угроза. Весь персонал обязан немедленно эвакуироваться.

# Действия
blob-teleport-to-node-action-name = Прыжок к узлу (0)
blob-teleport-to-node-action-desc = Телепортирует вас к случайному узлу блоба.
blob-help-action-name = Помощь
blob-help-action-desc = Получить базовую информацию об игре за блоба.

# Роль призрака
blob-carrier-role-name = Носитель блоба
blob-carrier-role-desc = Существо, заражённое блобом.
blob-carrier-role-rules = Вы антагонист. У вас есть 35 минут до превращения в блоба.
                        Используйте это время, чтобы найти безопасное место на станции. Помните, что сразу после превращения вы будете очень слабы.
blob-carrier-role-greeting = Вы носитель Блоба. Найдите укромное место на станции и превратитесь в Блоба. Превратите станцию в единую массу, а её обитателей — в своих слуг. Мы все — Блобы.

# Действия (verbs)
blob-pod-verb-zombify = Зомбифицировать
blob-verb-upgrade-to-strong = Улучшить до укреплённого блоба
blob-verb-upgrade-to-reflective = Улучшить до отражающего блоба
blob-verb-remove-blob-tile = Убрать блоб

# Алерты
blob-resource-alert-name = Ресурсы ядра
blob-resource-alert-desc = Ваши ресурсы, производимые ядром и ресурсными блобами. Используйте их для роста и создания специальных блобов.
blob-health-alert-name = Здоровье ядра
blob-health-alert-desc = Здоровье вашего ядра. Вы погибнете, если оно достигнет нуля.

# Приветствие
blob-role-greeting =
    Вы блоб — паразитическое космическое существо, способное уничтожить целые станции.
        Ваша цель — выжить и разрастись как можно сильнее.
        Вы почти неуязвимы к физическому урону, но огонь всё ещё может вам навредить.
        Используйте Alt+ЛКМ, чтобы улучшать обычные тайлы блоба до укреплённых, а укреплённые — до отражающих.
        Не забывайте размещать ресурсные блобы для производства ресурсов.
        Помните, что ресурсные блобы и фабрики работают только рядом с узлами блоба или ядром.
        Вы можете использовать + или +e в чате, чтобы говорить со своими прислужниками через Разум блоба.
blob-zombie-greeting = Вас заразила и подняла спора блоба. Теперь вы должны помочь блобу захватить станцию. Используйте +e в чате, чтобы говорить в Разуме блоба.

# Конец раунда
blob-round-end-result =
    { $blobCount ->
        [one] На станции было одно заражение блобом.
        *[other] На станции было заражений блобом: { $blobCount }.
    }

blob-user-was-a-blob = [color=gray]{$user}[/color] был блобом.
blob-user-was-a-blob-named = [color=White]{$name}[/color] ([color=gray]{$user}[/color]) был блобом.
blob-was-a-blob-named = [color=White]{$name}[/color] был блобом.

preset-blob-objective-issuer-blob = [color=#33cc00]Блоб[/color]

blob-user-was-a-blob-with-objectives = [color=gray]{$user}[/color] был блобом со следующими целями:
blob-user-was-a-blob-with-objectives-named = [color=White]{$name}[/color] ([color=gray]{$user}[/color]) был блобом со следующими целями:
blob-was-a-blob-with-objectives-named = [color=White]{$name}[/color] был блобом со следующими целями:

# Цели
objective-condition-blob-capture-title = Захватить станцию
objective-condition-blob-capture-description = Ваша единственная цель — захватить всю станцию. Вам нужно как минимум { $count } тайлов блоба.
objective-condition-success = { $condition } | [color={ $markupColor }]Успех![/color]
objective-condition-fail = { $condition } | [color={ $markupColor }]Провал![/color] ({ $progress }%)

# Команды администратора
admin-verb-make-blob = Превратить цель в носителя блоба.
admin-verb-text-make-blob = Сделать носителем блоба

# Язык
language-ADTBlob-name = Блоб
chat-language-ADTBlob-name = Блоб
language-ADTBlob-description = Блиб блоб! Блоб блоб!
