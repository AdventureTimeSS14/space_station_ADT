alerts-shadowling-light-name = Свет
alerts-shadowling-light-desc = Запас тьмы в вашем теле. Зелёная лампа - вы в безопасности, красная - вы уже горите. На свету запас тает, в темноте восстанавливается.

shadowling-tumor-collapses = { $tumor } схлопывается сама в себя!

shadowling-thrall-examine = Черты лица неестественно натянуты.

shadowling-not-hatched = Ваши телепатические способности подавлены. Сначала сбросьте маскировку.

# Hatch
shadowling-round-duration-cooldown = Вы сможете раскрыться через { $minutes } мин.
shadowling-hatch-need-floor = Чтобы раскрыться, нужно стоять на полу!
shadowling-hatch-begin-self = Вы сбрасываете всё, что может помешать вылуплению, и начинаете выделять смолу, которая вас защитит.
shadowling-hatch-begin-others = Вещи { $user } внезапно сползают. С них стекает обильная фиолетовая жижа, которая начинает формироваться вокруг.
shadowling-hatch-stage = { $stage ->
        [0] Вы обвиваетесь в хризалиду и начинаете извиваться внутри.
        [1] Шипы пронзают вашу спину. Когти разрывают ваши пальцы. Истинная форма начинает свой выход.
        [2] Фальшивая кожа отваливается. Вы начинаете рвать защищающую вас хрупкую мембрану.
       *[3] Вы рвёте и режете. Хризалида осыпается перед вами, как капли воды.
    }
shadowling-hatch-alive = ВЫ ЖИВЫ!!! Ваши силы пробудились.

# Glare
shadowling-glare-cast = Глаза { $user } вспыхивают ослепительно красным светом!
shadowling-glare-close = Ваш взгляд насильно притягивается к чужим глазам, и вы пленяетесь их неописуемой красотой...
shadowling-glare-far = Красный свет вспыхивает перед глазами, разум пытается ему противостоять. Вы обессилены и не можете говорить.

# Veil
shadowling-veil-cast = Вы бесшумно гасите все ближайшие источники света.

# Shadow Walk
shadowling-shadow-walk-enter = { $user } исчезает в клубах чёрного тумана!

# Icy Veins
shadowling-icy-veins-cast = Вы замораживаете воздух поблизости.
shadowling-icy-veins-hit = Вас накрывает волна невероятно холодного воздуха!
shadowling-icy-veins-immune = Порыв ледяного воздуха обволакивает вас и проносится мимо, но вас это не затрагивает.

# Enthrall
shadowling-enthrall-invalid = Эта цель не подходит.
shadowling-enthrall-mindshield = У этой цели щит разума, блокирующий ваши силы. Подчинить её нельзя.
shadowling-enthrall-begin-self = Цель верна. Вы начинаете процесс подчинения.
shadowling-enthrall-begin-target = { $user } смотрит на вас. Вы чувствуете, как ваша голова начинает пульсировать.
shadowling-enthrall-stage = { $stage ->
        [1] Вы прикладываете ладони к голове цели...
        [2] Вы начинаете обрабатывать разум цели до состояния чистого листа...
       *[3] Вы начинаете выращивать опухоль, которая будет контролировать нового раба...
    }
shadowling-enthrall-collapse = Ужасный красный свет заливает ваш разум. Вы падаете, когда сознание стирается.
shadowling-enthrall-interrupted-self = Порабощение прервано, разум цели возвращается в прежнее состояние.
shadowling-enthrall-interrupted-target = Вы вырываетесь из цепких рук { $user } и приходите в себя.
shadowling-enthrall-success = Вы подчинили себе { $target }!
shadowling-enthrall-converted = Фальшивые лица все ТЁМНЫЕ не настоящие не настоящие не...

# Guise
shadowling-guise-enter = Вы окутываете себя тьмой, и вас становится трудно разглядеть.
shadowling-guise-exit = Ваша теневая маскировка исчезает.

species-name-adt-shadowling = Тенеморф
species-name-adt-lesser-shadowling = Усиленный раб

ent-ADTMobShadowling = тенеморф
    .desc = Сгорбленный хитиновый ужас. Его глаза горят густым багрянцем, а воздух вокруг словно выпивает свет.

ent-ADTMobLesserShadowling = усиленный раб
    .desc = Когда-то это был человек. Кожа посерела и натянулась, а движения такие, будто тело ему не по размеру.

ent-ADTMobAscendantShadowling = вознёсшийся тенеморф
    .desc = Огромный парящий ужас. По телу пульсируют метки, рога скребут по потолку. Его ничто не держит.

ent-ADTShadowlingChrysalis = стена хризалиды
    .desc = Фиолетовая субстанция в форме яйца. Она пульсирует изнутри и выглядит непробиваемой.

ent-ADTShadowTumor = чёрная опухоль
    .desc = Небольшой чёрный сгусток с тянущимися от него красными щупальцами. На свету он сморщивается.

# Collective Hivemind
shadowling-hivemind-begin = Вы тянетесь телепатическими нитями и собираете силу своих рабов.
shadowling-hivemind-interrupted = Ваша концентрация нарушена. Мысленные крючья втягиваются обратно.
shadowling-hivemind-unlocked = Сила рабов открыла вам новую способность.
shadowling-hivemind-count = Вам не хватает сил для вознесения. Нужно { $needed } рабов, а живых сейчас { $current }.
shadowling-hivemind-ascension-ready = Силы достаточно. Вы можете вознестись, когда будете готовы.

# Blinding Smoke
shadowling-smoke-self = Вы изрыгаете огромное облако слепящего дыма.
shadowling-smoke-others = { $user } резко сгибается и выкашливает облако чёрного дыма, которое стремительно расползается!

# Sonic Screech
shadowling-screech-cast = { $user } издаёт нечеловеческий вопль!
shadowling-screech-hit = Острая боль пронзает голову и путает мысли!
shadowling-screech-silicon = ОШИБКА $!(@ ОШИБКА )#^! ПЕРЕГРУЗКА СЕНСОРОВ \[$(!@#

# Null Charge
shadowling-null-charge-invalid = Это не ЛКП.
shadowling-null-charge-empty = В этом ЛКП нечего сливать.
shadowling-null-charge-done = Заряд ушёл в пустоту, а автомат вырублен.

# Black Recuperation
shadowling-recuperation-not-thrall = Эта цель вам не служит.
shadowling-recuperation-already-empowered = Этот раб уже наделён силой.
shadowling-recuperation-too-many = Вы не можете тратить столько энергии. Наделённых силой рабов слишком много.
shadowling-recuperation-empower-begin = Вы кладёте ладони на лицо раба и начинаете наполнять его энергией...
shadowling-recuperation-revive-begin = Вы склоняетесь над телом своего раба и начинаете накапливать энергию...
shadowling-recuperation-empowered = Вы чувствуете, как в вас вливается новая сила. Вы получили дар от своих хозяев.
shadowling-recuperation-revived = Вы вернулись. Один из хозяев вытащил вас из потусторонней тьмы.

# Destroy Engines
shadowling-destroy-engines-no-shuttle = Шаттл ещё не в пути, тянуть нечего.
shadowling-destroy-engines-begin = Вы начинаете вытягивать жизненную силу цели.
shadowling-destroy-engines-victim = Вас внезапно уносит... далеко-далеко...
shadowling-destroy-engines-interrupted = Вас прервали. Вытягивание сорвалось.
shadowling-destroy-engines-announcement = Крупный системный сбой на борту эвакуационного шаттла. Время прибытия увеличено примерно на десять минут, отозвать шаттл невозможно.
shadowling-destroy-engines-announcer = Системный сбой

# Reagent
reagent-name-adt-shadowling-smoke = странная чёрная жидкость
reagent-desc-adt-shadowling-smoke = ЗАПИСЬ В БАЗЕ ДАННЫХ ОТСУТСТВУЕТ
reagent-physical-desc-adt-shadowling-smoke = непроглядное

# Ascension
shadowling-ascend-begin-self = Вы взмываете в воздух и готовитесь к трансформации.
shadowling-ascend-begin-others = { $user } плавно поднимается в воздух, из глаз бьёт красный свет.
shadowling-ascend-stage = { $stage ->
        [0] Ваша кожа трескается и твердеет, становясь щитом.
        [1] Рожки на голове растут и вытягиваются. Телепатические силы крепнут.
        [2] Тело начинает неистово растягиваться и выгибаться.
       *[3] Вы разрушаете последние врата к божественности. ДА!!!
    }
shadowling-ascend-shockwave = Чудовищное давление вбивает вас в пол!
shadowling-ascension-announcement = Сканеры дальнего действия зафиксировали всплеск психической блюспейс-энергии. На борту произошло вознесение. Эвакуация неизбежна.
shadowling-ascension-announcer = Отдел паранормальных явлений ЦК
shadowling-ascension-evacuation = Вознесение на борту подтверждено. Эвакуационный шаттл вызван принудительно и не может быть отозван.

# Ascendant powers
shadowling-annihilate-ally = Взрывать союзника - не лучшая мысль.
shadowling-annihilate-cast = { $user } делает жест в сторону { $target }, и метки на его теле вспыхивают!
shadowling-hypnosis-cast = Вы мгновенно перекраиваете память { $target }, обращая цель в раба.
shadowling-hypnosis-victim = Волна мучительной боли проникает в ваше сознание, и...
shadowling-phase-shift-enter = { $user } внезапно исчезает!
shadowling-phase-shift-exit = Вы возвращаетесь из пространства между мирами.
shadowling-lightning-cast = В руках { $user } возникает и разлетается огромная шаровая молния!
shadowling-lightning-hit = Вас поражает молния!
shadowling-broadcast-title = Обратиться ко всем
shadowling-broadcast-prompt = Что вы хотите сказать всей станции?
shadowling-broadcast-sender = { $name }

# Dethralling
shadowling-dethrall-needs-down = Цель должна лежать, иначе к её голове не подобраться.
shadowling-dethrall-begin = Вы направляете свет в глаза { $target } и ищете опухоль.
shadowling-dethrall-success = Вы выжигаете заразу из головы { $target }.
shadowling-dethrall-freed = Пронзительный белый свет заполняет ваш разум. Он снова ваш, но о времени в рабстве вы не помните ничего.
shadowling-dethrall-backlash-thrall = ВОТ ТАК ПРОСТО?! НУ УЖ НЕТ!
shadowling-dethrall-backlash-surgeon = { $target } резко выгибается и сбивает вас с ног!

# Round
shadowling-briefing-intro = Вы - тенеморф. Пока вы носите чужое лицо и умеете лишь одно: сбросить его. Свет для вас смертелен, тьма исцеляет.
shadowling-briefing-objective = Обратите { $count } членов экипажа в рабов и вознеситесь. Другие тенеморфы - ваши союзники.
shadowling-warning-announcement = Сканерами дальнего действия обнаружена высокая концентрация психической блюспейс-энергии. Вероятность вознесения крайне высока. Экипажу предписано предотвратить его любой ценой.
shadowling-warning-announcer = Отдел паранормальных явлений ЦК
shadowling-roundend-agent-name = тенеморф
shadowling-roundend-ascended = [color=red]Тенеморфы вознеслись и захватили станцию.[/color]
shadowling-roundend-failed = [color=green]Ни один тенеморф не сумел вознестись.[/color]
shadowling-roundend-thralls = Живых рабов на конец раунда: { $count } из { $needed } необходимых.

roles-antag-adt-shadowling-name = Тенеморф
roles-antag-adt-shadowling-objective = Обратите экипаж в рабов и вознеситесь до своей истинной формы.

adt-deafness-recovered = Звон в ушах наконец стихает.

reagent-name-adt-frostoil = Морозное масло
reagent-desc-adt-frostoil = Масло, сильно охлаждающее тело. Добывается из ледяных перцев.
reagent-physical-desc-adt-frostoil = студёное
adt-deafness-total = [color=gray][italic]Вы ничего не слышите...[/italic][/color]
adt-deafness-partial = [color=gray][italic]Вы слышите что-то про "{ $word }"...[/italic][/color]

admin-verb-text-make-adt-shadowling = Сделать тенеморфом
admin-verb-make-adt-shadowling = Превращает цель в тенеморфа. Начнёт замаскированным, со способностью раскрыться.

shadowling-shadow-walk-failed = Тьма не принимает вас сейчас.

objective-issuer-adt-shadowling = [color=#8a2be2]Зов тьмы[/color]

ent-ADTShadowlingAscendObjective = Вознестись
    .desc = Обратите экипаж в рабов и примите истинную форму. Когда вознесётся хотя бы один из вас, победа общая.

guide-entry-adt-shadowling = Тенеморф

language-ADTShadowlingCollectiveMind-name = Коллективный разум
language-ADTShadowlingCollectiveMind-description = Телепатическая связь улья тенеморфов. Слышат её только сами тенеморфы и их рабы.

adt-shadowling-title = Тенеморфы
adt-shadowling-description = Среди экипажа прячутся тенеморфы. Они обращают людей в рабов, гасят свет по всей станции и готовятся к вознесению.

ent-ADTShadowlingGameRule = тенеморфы
    .desc = Выбирает тенеморфов среди экипажа в начале смены.
ent-ADTShadowlingSpawn = появление тенеморфа
    .desc = Обращает одного из членов экипажа в тенеморфа посреди смены.

ent-MindRoleShadowling = роль тенеморфа
    .desc = { "" }

ent-ADTMobShadowlingShadowWalk = тень
    .desc = Едва заметное движение между стен.

ent-ADTMobAscendantPhased = тень
    .desc = Разрыв в воздухе там, где только что было нечто огромное.
