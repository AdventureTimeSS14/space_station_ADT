# SPDX-FileCopyrightText: 2026 ultradyper <ultradyper@users.noreply.github.com>
#
# SPDX-License-Identifier: AGPL-3.0-or-later

trait-alcoholic-addiction-name = Алкогольная зависимость
trait-alcoholic-addiction-desc = Ты не можешь жить без выпивки и стартуешь с тяжёлой стадией зависимости. Без дозы организм начинает ломаться, и с каждым часом воздержания становится только хуже. Эта зависимость неизлечима, с ней придётся жить.

trait-nicotine-addiction-name = Никотиновая зависимость
trait-nicotine-addiction-desc = Организм требует сигарету, и стартуешь ты с тяжёлой стадией зависимости. Без никотина руки трясутся, настроение падает, а мысли крутятся вокруг одной затяжки. Эта зависимость неизлечима, с ней придётся жить.

trait-drug-addiction-name = Наркозависимость
trait-drug-addiction-desc = Ты зависим от наркотиков и стартуешь с тяжёлой стадией зависимости. Без дозы мир становится серым и враждебным, а в тяжёлой стадии начинаются галлюцинации. Эта зависимость неизлечима, с ней придётся жить.

trait-random-addiction-name = Случайная зависимость
trait-random-addiction-desc = Ты не знаешь, к чему именно пристрастился, но без дозы тебе точно будет плохо. При спавне получишь одну случайную зависимость из трёх: алкоголь, никотин или наркотики, сразу с тяжёлой стадией. Нельзя выбрать, если уже выбраны все три конкретные зависимости. Эта зависимость неизлечима, с ней придётся жить.

addiction-begin-alcohol = Ты замечаешь, что начинаешь привыкать к алкоголю...
addiction-begin-nicotine = Ты замечаешь, что начинаешь привыкать к никотину...
addiction-begin-drug = Ты замечаешь, что начинаешь привыкать к наркотикам...
addiction-begin-medicine = Ты замечаешь, что начинаешь привыкать к лекарствам...

addiction-dose-alcohol = Глоток. По телу разливается тепло, ломка отступает.
addiction-dose-nicotine = Затяжка. Голова проясняется, руки перестают трястись.
addiction-dose-drug = Доза ударила в голову. Ломка отступает.
addiction-dose-medicine = Доза лекарства. Ломка отступает.

addiction-withdrawal-alcohol-0 = Руки слегка трясутся. Неплохо бы выпить...
addiction-withdrawal-alcohol-1 = Всё раздражает, слова путаются. Без выпивки невыносимо.
addiction-withdrawal-alcohol-2 = Тело ломит, мысли путаются. Нужна доза. Срочно.
addiction-withdrawal-nicotine-0 = Хочется курить. Очень.
addiction-withdrawal-nicotine-1 = Пальцы чешутся, настроение на нуле. Нужна сигарета.
addiction-withdrawal-nicotine-2 = Голова раскалывается, руки ходят ходуном. Сигарета. Сейчас же.
addiction-withdrawal-drug-0 = Мир кажется пресным. Нужна доза.
addiction-withdrawal-drug-1 = Тревога нарастает, слова путаются. Без наркотика невыносимо.
addiction-withdrawal-drug-2 = Галлюцинации на грани, ноги подкашиваются. Доза. Немедленно.
addiction-withdrawal-medicine-0 = Становится не по себе. Нужна доза лекарства.
addiction-withdrawal-medicine-1 = Тело ноет, настроение на нуле. Без лекарства невыносимо.
addiction-withdrawal-medicine-2 = Ломка скручивает, мысли путаются. Лекарство. Сейчас же.

addiction-cured-alcohol = Зависимость от алкоголя отпустила тебя.
addiction-cured-nicotine = Зависимость от никотина отпустила тебя.
addiction-cured-drug = Зависимость от наркотиков отпустила тебя.
addiction-cured-medicine = Зависимость от лекарств отпустила тебя.

reagent-name-adt-detoxin = Детоксин
reagent-desc-adt-detoxin = Снимает абстинентный синдром и постепенно лечит зависимость от алкоголя, никотина, наркотиков и лекарств. Бессилен против врождённых зависимостей (трайтов).
reagent-physical-desc-pungent = Резко пахнет лекарствами

entity-effect-guidebook-adjust-addiction-level = Снижает уровень привыкания на {$amount} за цикл метаболизма.

guide-entry-addictions = Зависимости

health-analyzer-window-addictions-title = Зависимости:
health-analyzer-addiction-alcohol = Алкогольная
health-analyzer-addiction-nicotine = Никотиновая
health-analyzer-addiction-drug = Наркотическая
health-analyzer-addiction-medicine = Лекарственная
health-analyzer-window-addiction-line = { $kind } зависимость: стадия { $stage } ({ $stageName })
health-analyzer-window-addiction-permanent = { $kind } зависимость: стадия { $stage } ({ $stageName }, не лечится)
health-analyzer-addiction-stage-1 = лёгкая
health-analyzer-addiction-stage-2 = средняя
health-analyzer-addiction-stage-3 = тяжёлая
health-analyzer-window-addictions-treatment = Лечение: воздержание или Детоксин
health-analyzer-window-addictions-untreatable = Лечение невозможно: зависимость хроническая
