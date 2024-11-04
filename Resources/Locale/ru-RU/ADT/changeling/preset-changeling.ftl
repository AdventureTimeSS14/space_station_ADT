## CHANGELINGS

adt-rotting-ling-eggs = [color=#ffd6d6]Внутри тела видны странные личинки...[/color]
ling-round-end-name = генокрад
objective-issuer-changeling = [color=red]Разум улья[/color]
changelings-title = Генокрады
changelings-description = дурацкие воришки ДНК
changelings-not-enough-ready-players = Недостаточно игроков нажали "Готов"! Всего было {$readyPlayersCount} готовых игроков из {$minimumPlayers} нужных. Режим "Генокрады" не запустится.
changelings-no-one-ready = Никто не нажал "Готов"! Режим "Генокрады" начать нельзя.
# Ling role
changeling-role-greeting =
    Вы являетесь Генокрадом, впитавшим форму {$character-name} и превратившимся в него.
    Ваши цели перечислены в специальном меню, используйте ваши способности, чтобы выполнить их.
changeling-role-greeting-short =
    Вы являетесь Генокрадом, впитавшим форму
    {$character-name} и превратившимся в него.
    Используйте ваши способности, чтобы выполнить цели.
# Ling abilities
changeling-not-enough-chemicals = Недостаточно химических веществ!
changeling-armblade-fail = Вам нужна свободная рука!
changeling-armblade-success-others = Вокруг руки {THE($user)} образуется причудливое лезвие!
changeling-armblade-success-self = Ваша рука изгибается и мутирует, превращаясь в смертоносный клинок.
changeling-armblade-retract-others = Клинок {CAPITALIZE(THE($target))} впитывается обратно в руку!
changeling-armblade-retract-self = Вы впитываете клинок обратно в свою руку.
changeling-armor-success-others = Тело {CAPITALIZE(THE($target))} набирает бронированную защитную массу!
changeling-armor-success-self = Ваше тело набирает защитную массу.
changeling-armor-retract-others = Броня {CAPITALIZE(THE($target))} отслаивается.
changeling-armor-retract-self = Ваша броня отслаивается.
changeling-regenerate-others-success = {CAPITALIZE(THE($user))} быстро излечивает все раны, издавая громкий, странный звук!
changeling-regenerate-self-success = Вы чувствуете зуд, как внутри, так и снаружи, когда ваши ткани восстанавливаются снова и снова!
changeling-regenerate-fail-not-crit = Вы не в критическом состоянии!
changeling-regenerate-fail-dead = Вы мертвы!
changeling-chameleon-toggle-on = Ваша кожа переливается прозрачной оболочкой...
changeling-chameleon-toggle-off = Ваша кожа возвращается к нормальному виду.
changeling-dna-stage-1 = Это существо подходит для взятия ДНК. Вы должны стоять неподвижно...
changeling-dna-stage-2-self = Вы вытягиваете жало.
changeling-dna-stage-2-others = {CAPITALIZE(THE($user))} вытягивает жало!
changeling-dna-stage-3-self = Вы укалываете {THE($target)} своим жалом!
changeling-dna-stage-3-others = {CAPITALIZE(THE($user))} укалывает {THE($target)} жалом!
changeling-dna-success = Вы впитали ДНК {THE($target)}.
changeling-dna-success-ling = Вы впитали ДНК {THE($target)}. Это был другой генокрад! Вы набрали 5 очков эволюции.
changeling-dna-fail-nohuman = {CAPITALIZE(THE($target))} не гуманоид.
changeling-dna-fail-notdead = {CAPITALIZE(THE($target))} не критическом состоянии или мёртв.
changeling-dna-interrupted = Поглощение ДНК {THE($target)} было прервано.
changeling-dna-alreadyabsorbed = ДНК {CAPITALIZE(THE($target))} уже собраны!
changeling-dna-nodna = {CAPITALIZE(THE($target))} не имеет ДНК!
changeling-dna-switchdna = Переключился на ДНК {$target}.
changeling-transform-activate = Вы превратились в {$target}.
changeling-transform-fail-already = Вы уже превратились в {$target}!
changeling-transform-fail-mutation = Ваше тело слишком искажено для превращения!
changeling-sting-fail-self = Ужалить {THE($target)} не вышло!
changeling-sting-fail-target = Вы чувствуёте лёгкое покалывание.
changeling-dna-sting = Вы забираете образец ДНК {THE($target)}.
changeling-dna-sting-fail-nodna = {CAPITALIZE(THE($target))} не имеет ДНК!
changeling-dna-sting-fail-alreadydna = У вас уже есть ДНК {THE($target)}!
changeling-stasis-death-self-success = Вы впадаете в стазис.
changeling-stasis-death-self-revive = Вы восстаёте из мёртвых!
changeling-refresh-not-ready = Сначала поглотите существо.
changeling-adrenaline-self-success = Боль начинает уходить.
changeling-refresh-self-success = Успешно очищена цепочка ДНК.
changeling-omnizine-self-success = Ваша плоть начинает восстанавливаться.
changeling-success-sting = Вы успешно ужалили {CAPITALIZE(THE($target))}!
changeling-dna-sting-fail-full = Вы не сможете собрать ещё одну цепочку ДНК!
changeling-lingmuscles = Ваши мышцы быстро меняются!
changeling-lesser-form-activate-monkey = Вы превратились в обезьяну!
changeling-transform-fail-lesser-form = Вы не можете использовать данную способность в этой форме!
changeling-armshield-fail = Нужна свободная рука!
changeling-armshield-success-others = Вокруг руки {THE($user)} образуется массивный щит!
changeling-armshield-success-self = Ваша рука изгибается и мутирует, превращаясь в огромный массивный щит.
changeling-armshield-retract-others = Щит {CAPITALIZE(THE($target))} впитывается обратно в руку!
changeling-armshield-retract-self = Вы впитываете щит обратно в свою руку.
changeling-armshield-broke-others = Щит {CAPITALIZE(THE($user))} разорвался и впитался обратно!
changeling-armshield-broke-self = Ваш щит был пробит!
changeling-eggs-self-start = Вы начинаете откладывать личинки в {CAPITALIZE(THE($target))}.
changeling-eggs-self-success = Вы успешно оставили кладку в {CAPITALIZE(THE($target))}.
changeling-eggs-interrupted = Кладка была прервана!
changeling-egg-others = Личинки в теле {THE($user)} начинают активно шевелиться и разрастаться!
changeling-egg-self = Личинки прорастают, готовясь вырваться наружу.
changeling-biodegrade-start = Вы начинаете расплавлять наручники.
changeling-biodegrade-interrupted = Освобождение было прервано!
changeling-biodegrade-fail-nocuffs = На вас нет наручников.
changeling-transform-fail-nodna = У вас нет сохранённых ДНК!
changeling-armace-success-others = Кости и плоть руки {THE($user)} преобразуются в огромный молот!
changeling-armace-success-self = Ваша рука изгибается и мутирует, превращаясь в огромный молот.
changeling-armace-retract-others = Молот {CAPITALIZE(THE($target))} впитывается обратно в руку!
changeling-armace-retract-self = Вы впитываете молот обратно в свою руку.
defibrillator-changeling-slug = Внутри тела обнаружен паразит.
