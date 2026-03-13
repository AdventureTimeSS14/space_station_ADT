cmd-languageadd-desc = Добавляет язык сущности.
cmd-languageadd-help = Использование: languageadd <entity uid> <язык> [уровень]
cmd-languageadd-success = Язык { $language } ({ $knowledge }) добавлен сущности { $entity }.
cmd-languageadd-invalid-language = Язык { $language } не найден.
cmd-languageadd-invalid-knowledge = Неверный уровень знания языка. Используйте: Understand, BadSpeak, Speak.
cmd-languageadd-no-language-component = У сущности нет компонента языка.

cmd-languageslist-desc = Показывает список языков, которые знает сущность.
cmd-languageslist-help = Использование: languageslist <entity uid>
cmd-languageslist-header = Языки сущности { $entity }:
cmd-languageslist-empty = Сущность не знает ни одного языка.
cmd-languageslist-no-language-component = У сущности { $entity } нет компонента языка.
cmd-languageslist-line =  - { $proto } ({ $id }): { $knowledge }{ $current }
cmd-languageslist-current-suffix =  [Текущий]

cmd-languageremove-desc = Удаляет язык у сущности.
cmd-languageremove-help = Использование: languageremove <entity uid> <язык>
cmd-languageremove-success = Язык { $language } удалён у сущности { $entity }.
cmd-languageremove-invalid-language = Язык { $language } не найден.
cmd-languageremove-no-language-component = У сущности { $entity } нет компонента языка.
cmd-languageremove-not-known = Сущность { $entity } не знает язык { $language }.

cmd-language-hint = Язык
cmd-knowledge-hint = Уровень
