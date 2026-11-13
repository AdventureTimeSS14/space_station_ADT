adt-fracture-type-crack = Микротрещина
adt-fracture-type-closed = Закрытый перелом
adt-fracture-type-open = Открытый перелом

adt-fracture-self = Вы чувствуете, как что-то хрустнуло внутри! Сломано: { $limb }.
adt-fracture-others = Вы слышите громкий хруст, исходящий от { $target }.
adt-fracture-moving-pain = Вы чувствуете, как при движении смещаются сломанные кости!
adt-fracture-fall-self = Сломанная нога подкашивается, и вы падаете!
adt-fracture-fall-others = { CAPITALIZE($target) } падает, не устояв на сломанной ноге!
adt-fracture-drop-self = Острая боль в сломанной руке, и вы роняете { $item }!
adt-fracture-drop-others = { CAPITALIZE($target) } вскрикивает и роняет { $item }!

adt-fracture-reattach-start = { CAPITALIZE($user) } пытается вправить торчащую кость. Затронуто: { $limb }.
adt-fracture-reattach-success = Вам удаётся вправить кость, перелом стал закрытым. Затронуто: { $limb }.
adt-fracture-reattach-fail = Кость соскальзывает обратно, причиняя ужасную боль. Затронуто: { $limb }.

adt-fracture-examine-splint = [color=lightblue]На конечности { $target } наложена шина: { $limb }.[/color]
adt-fracture-examine-open = [color=red]Из тела { $target } торчит сломанная кость: { $limb }![/color]

adt-splint-start = { CAPITALIZE($user) } начинает накладывать вам шину. Затронуто: { $limb }.
adt-splint-applied = { CAPITALIZE($user) } накладывает шину. Затронуто: { $limb }.
adt-splint-removed = Вы снимаете шину. Затронуто: { $limb }.
adt-splint-broke-self = Шина спадает, и сломанная кость отзывается дикой болью! Затронуто: { $limb }.
adt-splint-broke-others = Шина спадает с { $target }, заставляя кричать от боли! Затронуто: { $limb }.

adt-shrapnel-embed-self = В ваше тело впивается { $item }! Затронуто: { $limb }.
adt-shrapnel-pain = Застрявший осколок причиняет боль! Затронуто: { $limb }.
adt-shrapnel-fall = Из тела { $target } выпадает { $item }!
adt-shrapnel-remove-start = { CAPITALIZE($user) } пытается извлечь осколок. Затронуто: { $limb }.
adt-shrapnel-remove-fail = Осколок срывается и глубже впивается в плоть. Затронуто: { $limb }.
adt-shrapnel-removed = { CAPITALIZE($user) } извлекает { $item }. Затронуто: { $limb }.
adt-shrapnel-examine = [color=red]В теле { $target } застряли осколки ({ $count } шт.): { $limb }.[/color]

ent-ADTMedicalSplint = медицинская шина
    .desc = Стандартная медицинская шина, предназначенная для иммобилизации сломанных конечностей до получения полноценной медицинской помощи.
    .suffix = Полный
ent-ADTMedicalSplint1 = { ent-ADTMedicalSplint }
    .desc = { ent-ADTMedicalSplint.desc }
    .suffix = Один
ent-ADTMakeshiftSplint = импровизированная шина
    .desc = Самодельная шина из пары деревянных палок, перевязанных кусками ткани. Сильно уступает стандартным аналогам.
    .suffix = Полный
ent-ADTMakeshiftSplint1 = { ent-ADTMakeshiftSplint }
    .desc = { ent-ADTMakeshiftSplint.desc }
    .suffix = Один
ent-ADTShrapnel = шрапнель
    .desc = Зазубренный кусок металла. Лучше снаружи, чем внутри.

adt-health-analyzer-fracture = { $type }: { $limb }.
adt-health-analyzer-fracture-splinted = { $type } (наложена шина): { $limb }.
adt-health-analyzer-shrapnel = Инородные объекты ({ $count } шт.): { $limb }.

adt-fracture-mended-reagent = Вы чувствуете жжение, кость принимает правильную форму: { $limb }.
reagent-name-adt-nanocalcium = нано-кальций
reagent-desc-adt-nanocalcium = Высокотехнологичные наниты, восстанавливающие кости и органы. Первые минуты приносят невыносимые страдания. Смертельно опасны в сочетании со стимуляторами.
adt-nanocalcium-effect-burn = Ваша кожа горит, а кровь в венах закипает!
reagent-effect-guidebook-mend-fractures =
    { $chance ->
        [1] Сращивает
       *[other] сращивают
    } { $open ->
        [true] все переломы
       *[false] закрытые переломы и трещины
    }
ent-ADTNanocalciumAutoInjector = автоинъектор нано-кальция
    .desc = Экспериментальные наниты для сращивания костей. Первая минута будет очень неприятной. Не смешивать со стимуляторами.

adt-body-action-verb = Осмотреть травмы
adt-body-action-nothing = Здесь нечего делать.
adt-body-action-apply-splint = Наложить шину: { $limb }
adt-body-action-remove-splint = Снять шину: { $limb }
adt-body-action-reattach = Вправить кость: { $limb }
adt-body-action-shrapnel-hands = Вытащить осколок руками: { $limb }
adt-body-action-shrapnel-tool = Выковырять осколок ({ $tool }): { $limb }

alerts-adt-embedded-shrapnel-name = [color=red]Осколки в теле[/color]
alerts-adt-embedded-shrapnel-desc = В вас застряли осколки, и каждое движение отзывается болью. Нажмите, чтобы вытащить их руками или выковырять инструментом из активной руки.
