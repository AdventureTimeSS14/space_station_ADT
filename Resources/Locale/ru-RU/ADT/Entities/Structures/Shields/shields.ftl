ent-ADTShieldGenerator = продвинутый генератор щита
    .desc = Тяжёлый генератор щита с накопителем энергии, способный создавать энергетическое поле вокруг конструкции. Требует три развёрнутых конденсатора для запуска.

ent-ADTShieldGeneratorDebug = отладочный генератор щита
    .desc = Отладочная версия генератора щита для тестирования. Не требует опоры или питания и имеет практически бесконечную энергию.
    .suffix = Дебаг

ent-ADTShieldConduit = конденсатор щита
    .desc = Комбинированный кондуктор и конденсатор, передающий и накапливающий огромные объёмы энергии для генератора щита. Размещается рядом с генератором.

ent-ADTShieldSegment = энергетический щит
    .desc = Непроницаемое поле энергии, способное блокировать всё, пока оно активно.

ent-ADTShieldDiffuser = рассеиватель щита
    .desc = Небольшое устройство под обшивкой, специально предназначенное для разрушения энергетических барьеров. Рассеивает щит прямо над собой. Обычно устанавливается у внешних шлюзов, чтобы щит не мешал выходу в открытый космос.

ent-ADTShieldHandheldDiffuser = портативный рассеиватель щита
    .desc = Небольшое ручное устройство, предназначенное для разрушения энергетических барьеров. Рассеивает щиты в небольшом радиусе вокруг себя. Работает от внутренней батареи, которую можно зарядить в обычном зарядном устройстве.

# UI
shield-window-title = Генератор щита
shield-window-start = Включить генератор
shield-window-stop = Плавно выключить
shield-window-emergency = Аварийное выключение
shield-window-input-cap = Лимит потребления:
shield-window-input-cap-value = Лимит потребления: {$cap} кВт
shield-window-apply = Применить
shield-window-integrity = Целостность поля: {$integrity}%
shield-window-energy = Энергия: {$current} МДж / {$max} МДж
shield-window-upkeep = Расход на поддержание: {$amount} Вт
shield-window-segments = Сегменты: {$functional} / {$total}
shield-window-conduits = Конденсаторы: {$deployed} / {$required}

shield-status-running = Статус: поле активно
shield-status-discharging = Статус: выключение...
shield-status-off = Статус: выключено
shield-status-overloaded = Статус: ПЕРЕГРУЗКА

# Handheld diffuser examine
shield-handheld-enabled = Он включён.
shield-handheld-disabled = Он выключен.
shield-handheld-uses-left = Ему хватит заряда ещё примерно на {$amount} применений.

# Shield modes
shield-mode-hyperkinetic = Гиперкинетические снаряды
shield-mode-hyperkinetic-desc = Блокирует быстродвижущиеся физические объекты: пули, ударное оружие, метеоры.

shield-mode-photonic = Фотонная дисперсия
shield-mode-photonic-desc = Блокирует большинство энергетического оружия и лучей.

shield-mode-humanoids = Гуманоидные формы жизни
shield-mode-humanoids-desc = Блокирует гуманоидных существ. Не влияет на полностью синтетических гуманоидов.

shield-mode-anorganic = Кремниевые формы жизни
shield-mode-anorganic-desc = Блокирует кремниевых существ: киборгов, дронов, ИПС.

shield-mode-atmospheric = Атмосферная изоляция
shield-mode-atmospheric-desc = Блокирует поток воздуха и действует как изолятор атмосферы.

shield-mode-modulate = Адаптивная гармоника поля
shield-mode-modulate-desc = Модулирует частоты поля, позволяя ему адаптироваться к различным типам урона.

shield-mode-bypass = Обход рассеивателей
shield-mode-bypass-desc = Отключает предохранители, позволяя генератору противодействовать рассеивателям щита. Создаёт сильную нагрузку на генератор. Требует взлома.

shield-mode-overcharge = Перезарядка поля
shield-mode-overcharge-desc = Поляризует поле, нанося урон при контакте. Очень опасно для всего живого. Требует взлома.