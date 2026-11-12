adt-cmd-virology-hint-uid = <uid моба>
adt-cmd-virology-hint-disease = <adtDisease | advance>
adt-cmd-virology-hint-symptom = <adtSymptom>
adt-cmd-virology-no-entity = Сущность не найдена.
adt-cmd-virology-no-disease = Болезнь { $id } не найдена.
adt-cmd-virology-no-symptom = Симптом { $id } не найден.
adt-cmd-virology-done = Готово.

adt-cmd-infect-desc = Заражает сущность готовой болезнью или конструируемым вирусом из симптомов.
adt-cmd-infect-help = adtinfect <uid> <adtDisease> | adtinfect <uid> advance [adtSymptom...]
adt-cmd-infect-failed = Заразить не удалось: сущность невосприимчива, уже больна или имеет иммунитет.
adt-cmd-infect-done = Заражено: { $name } ({ $id }).

adt-cmd-cure-desc = Вылечивает все болезни сущности.
adt-cmd-cure-help = adtcure <uid> [true/false]
adt-cmd-cure-hint-immunity = <выдать иммунитет: true/false>

adt-cmd-diseases-desc = Показывает болезни и иммунитеты сущности.
adt-cmd-diseases-help = adtdiseases <uid>
adt-cmd-diseases-resistances = Иммунитет: { $list }

adt-cmd-stage-desc = Выставляет стадию болезни сущности.
adt-cmd-stage-help = adtdiseasestage <uid> <индекс> <стадия>
adt-cmd-stage-bad-index = Неверный индекс болезни, посмотрите список через adtdiseases.
adt-cmd-stage-hint-index = <индекс болезни>
adt-cmd-stage-hint-stage = <стадия>
