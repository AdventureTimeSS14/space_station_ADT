entity-effect-guidebook-spawn-entity =
    { $chance ->
        [1] Создаёт
        *[other] создаёт
    } { $amount ->
        [1] {INDEFINITE($entname)}
        *[other] {$amount} {MAKEPLURAL($entname)}
    }

entity-effect-guidebook-destroy =
    { $chance ->
        [1] Уничтожает
        *[other] уничтожает
    } объект
entity-effect-guidebook-break =
    { $chance ->
        [1] Ломает
        *[other] ломает
    } объект
entity-effect-guidebook-explosion =
    { $chance ->
        [1] Вызывает
        *[other] вызывает
    } взрыв

entity-effect-guidebook-emp =
    { $chance ->
        [1] Вызывает
        *[other] вызывает
    } электро-магнитный импульс

entity-effect-guidebook-flash =
    { $chance ->
        [1] Вызывает
        *[other] вызывает
    } ослепляющую вспышку

entity-effect-guidebook-foam-area =
    { $chance ->
        [1] Создаёт
        *[other] создаёт
    } большое колличество пены

entity-effect-guidebook-smoke-area =
    { $chance ->
        [1] Создаёт
        *[other] создаёт
    } большое колличество дыма

entity-effect-guidebook-satiate-thirst =
    { $chance ->
        [1] Утоляет
        *[other] утоляет
    } { $relative ->
        [1] немного жажду
        *[other] жажды в {NATURALFIXED($relative, 3)}x раз от нормального
    }

entity-effect-guidebook-satiate-hunger =
    { $chance ->
        [1] Утоляет
        *[other] утоляет
    } { $relative ->
        [1] немного голод
        *[other] голод в {NATURALFIXED($relative, 3)}x раз от нормального
    }

entity-effect-guidebook-health-change =
    { $chance ->
        [1] { $healsordeals ->
                [heals] Лечит
                [deals] Наносит
                *[both] Изменяет здоровье на
             }
        *[other] { $healsordeals ->
                    [heals] лечит
                    [deals] наносит
                    *[both] изменяет здоровье на
                 }
    } { $changes }

entity-effect-guidebook-even-health-change =
    { $chance ->
        [1] { $healsordeals ->
            [heals] Равномерно лечит
            [deals] Равномерно наносит
            *[both] Равномерно изменяет здоровье на
        }
        *[other] { $healsordeals ->
            [heals] Равномерно лечит
            [deals] Равномерно наносит
            *[both] Равномерно изменяет здоровье на
        }
    } { $changes }

entity-effect-guidebook-status-effect-old =
    { $type ->
        [update]{ $chance ->
                    [1] Вызывает
                     *[other] вызывает
                 } {LOC($key)} хотя бы на {NATURALFIXED($time, 3)} {MANY("секунд", $time)} без накопления
        [add]   { $chance ->
                    [1] Вызывает
                    *[other] вызывает
                } {LOC($key)} хотя бы на {NATURALFIXED($time, 3)} {MANY("секунд", $time)} с накоплением
        [set]  { $chance ->
                    [1] Вызывает
                    *[other] вызывает
                } {LOC($key)} на {NATURALFIXED($time, 3)} {MANY("секунд", $time)} без накопления
        *[remove]{ $chance ->
                    [1] Удаляет
                    *[other] удаляет
                } {NATURALFIXED($time, 3)} {MANY("секунд", $time)} от {LOC($key)}
    }

entity-effect-guidebook-status-effect =
    { $type ->
        [update]{ $chance ->
                    [1] Вызывает
                    *[other] вызывает
                 } {LOC($key)} минимум на {NATURALFIXED($time, 3)} { $time ->
                [one] секунду
                [few] секунды
               *[other] секунд
            }, эффект не накапливается
        [add]
            { $chance ->
                [1] Вызывает
               *[other] вызывает
            } { LOC($key) } минимум на { NATURALFIXED($time, 3) } { $time ->
                [one] секунду
                [few] секунды
               *[other] секунд
            }, эффект накапливается
        [set]
            { $chance ->
                [1] Вызывает
               *[other] вызывает
            } { LOC($key) } минимум на { NATURALFIXED($time, 3) } { $time ->
                [one] секунду
                [few] секунды
               *[other] секунд
            }, эффект не накапливается
        *[remove]
            { $chance ->
                [1] Удаляет
               *[other] удаляет
            } { NATURALFIXED($time, 3) } { $time ->
                [one] секунду
                [few] секунды
               *[other] секунд
            } от { LOC($key) }
    } { $delay ->
        [0] немедленно
        *[other] после { NATURALFIXED($delay, 3) } { $delay ->
            [one] секунду
            [few] секунды
            *[other] секунд
        } задержки
    }

entity-effect-guidebook-status-effect-indef =
    { $type ->
        [update]{ $chance ->
                    [1] Вызывает
                    *[other] вызывает
                 } постоянный {LOC($key)}
        [add]   { $chance ->
                    [1] Вызывает
                    *[other] вызывает
                } постоянный{LOC($key)}
        [set]  { $chance ->
                    [1] Вызывает
                    *[other] вызывает
                } постоянный{LOC($key)}
        *[remove]{ $chance ->
                    [1] Убирает
                    *[other] убирает
                } {LOC($key)}
    } { $delay ->
        [0] мгновенно
        *[other] после { NATURALFIXED($delay, 3) } { $delay ->
            [one] секунду
            [few] секунды
            *[other] секунд
        } задержки
    }

entity-effect-guidebook-knockdown =
    { $type ->
        [update]{ $chance ->
                    [1] Вызывает
                    *[other] вызывает
                    } {LOC($key)} хотя бы на {NATURALFIXED($time, 3)} {MANY("секунд", $time)} без накопления
        [add]   { $chance ->
                    [1] Вызывает
                    *[other] вызывает
                } оглушение хотя бы на {NATURALFIXED($time, 3)} {MANY("секунд", $time)} с накоплением
        *[set]  { $chance ->
                    [1] Вызывает
                    *[other] вызывает
                } оглушение хотя бы на {NATURALFIXED($time, 3)} {MANY("секунд", $time)} без накопления
        [remove]{ $chance ->
                    [1] Удаляет
                    *[other] удаляет
                } {NATURALFIXED($time, 3)} {MANY("секунд", $time)} оглушения
    }

entity-effect-guidebook-set-solution-temperature-effect =
    { $chance ->
        [1] Изменяет
        *[other] изменяет
    } температура раствора с точною к {NATURALFIXED($temperature, 2)}k

entity-effect-guidebook-adjust-solution-temperature-effect =
    { $chance ->
        [1] { $deltasign ->
                [1] Добавляет
                *[-1] Убавляет
            }
        *[other]
            { $deltasign ->
                [1] добавляет
                *[-1] убавляет
            }
    } тепло с раствора, пока она не будет равной { $deltasign ->
                [1] не больше {NATURALFIXED($maxtemp, 2)}k
                *[-1] не меньше {NATURALFIXED($mintemp, 2)}k
            }

entity-effect-guidebook-adjust-reagent-reagent =
    { $chance ->
        [1] { $deltasign ->
                [1] Добавляет
                *[-1] Убавляет
            }
        *[other]
            { $deltasign ->
                [1] добавляет
                *[-1] убавляет
            }
    } {NATURALFIXED($amount, 2)}u  {$reagent} { $deltasign ->
        [1] к
        *[-1] из
    } раствор

entity-effect-guidebook-adjust-reagent-group =
    { $chance ->
        [1] { $deltasign ->
                [1] Добавляет
                *[-1] Убавляет
            }
        *[other]
            { $deltasign ->
                [1] добавляет
                *[-1] убавляет
            }
    } {NATURALFIXED($amount, 2)}u реагентов из группы {$group} { $deltasign ->
            [1] к
            *[-1] из
        } раствор

entity-effect-guidebook-adjust-temperature =
    { $chance ->
        [1] { $deltasign ->
                [1] Добавляет
                *[-1] Убавляет
            }
        *[other]
            { $deltasign ->
                [1] добавляет
                *[-1] убавляет
            }
    } {POWERJOULES($amount)} тепла { $deltasign ->
            [1] к
            *[-1] из
        } в организм потребителя

entity-effect-guidebook-chem-cause-disease =
    { $chance ->
        [1] Вызывает
        *[other] вызывает
    } болезнь { $disease }

entity-effect-guidebook-chem-cause-random-disease =
    { $chance ->
        [1] Вызывает
        *[other] вызывает
    } болезни { $diseases }

entity-effect-guidebook-jittering =
    { $chance ->
        [1] Вызывает
        *[other] вызывает
    } тряску

entity-effect-guidebook-clean-bloodstream =
    { $chance ->
        [1] Очищает
        *[other] очищает
    } кровеносную систему от химикатов

entity-effect-guidebook-cure-disease =
    { $chance ->
        [1] Лечит
        *[other] лечат
    } болезнь

entity-effect-guidebook-eye-damage =
    { $chance ->
        [1] { $deltasign ->
                [1] Наносит
                *[-1] Лечит
            }
        *[other]
            { $deltasign ->
                [1] наносят
                *[-1] лечат
            }
    } урон зрению

entity-effect-guidebook-vomit =
    { $chance ->
        [1] Вызывает
        *[other] вызывает
    } рвоту

entity-effect-guidebook-create-gas =
    { $chance ->
        [1] Создаёт
        *[other] создаёт
    } { $moles } { $moles ->
        [1] моль
        *[other] моли
    } { $gas }

entity-effect-guidebook-drunk =
    { $chance ->
        [1] Вызывает
        *[other] вызывает
    } опьянение

entity-effect-guidebook-electrocute =
    { $chance ->
        [1] Электризует
        *[other] электризует
    } потребителя на {NATURALFIXED($time, 3)} {MANY("секунд", $time)}

entity-effect-guidebook-emote =
    { $chance ->
        [1] Точно заставит
        *[other] с определённым шансом заставит
    } потребителя сделать [bold][color=white]{$emote}[/color][/bold]

entity-effect-guidebook-extinguish-reaction =
    { $chance ->
        [1] Потушит
        *[other] потушит
    } огонь

entity-effect-guidebook-flammable-reaction =
    { $chance ->
        [1] Увеличит
        *[other] увеличит
    } поджигаемость

entity-effect-guidebook-ignite =
    { $chance ->
        [1] Поджигает
        *[other] поджигает
    } потребителя

entity-effect-guidebook-make-sentient =
    { $chance ->
        [1] Делает
        *[other] делает
    } потребителя разумным

entity-effect-guidebook-make-polymorph =
    { $chance ->
        [1] Полиморфирует
        *[other] полиморфирует
    } потребителя в { $entityname }

entity-effect-guidebook-modify-bleed-amount =
    { $chance ->
        [1] { $deltasign ->
                [1] Увеличивает
                *[-1] Уменьшает
            }
        *[other] { $deltasign ->
                    [1] увеличивает
                    *[-1] уменьшает
                 }
    } кровотечение

entity-effect-guidebook-modify-blood-level =
    { $chance ->
        [1] { $deltasign ->
                [1] Увеличивает
                *[-1] Уменьшает
            }
        *[other] { $deltasign ->
                    [1] увеличивает
                    *[-1] уменьшает
                 }
    } уровень крови

entity-effect-guidebook-paralyze =
    { $chance ->
        [1] Парализует
        *[other] парализует
    } потребителя не менее чем на {NATURALFIXED($time, 3)} {MANY("секунд", $time)}

entity-effect-guidebook-movespeed-modifier =
    { $chance ->
        [1] Изменяет
        *[other] изменяет
    } скорость передвижения в {NATURALFIXED($sprintspeed, 3)}x не менее чем на {NATURALFIXED($time, 3)} {MANY("секунд", $time)}

entity-effect-guidebook-reset-narcolepsy =
    { $chance ->
        [1] Времеенно останавливает
        *[other] времеенно останавливает
    } нарколесию

entity-effect-guidebook-wash-cream-pie-reaction =
    { $chance ->
        [1] Смывает
        *[other] смывает
    } кремовый пирог с лица обьекта

entity-effect-guidebook-cure-zombie-infection =
    { $chance ->
        [1] Лечит
        *[other] лечит
    } текущую зомби инфекцию

entity-effect-guidebook-cause-zombie-infection =
    { $chance ->
        [1] Даёт
        *[other] даёт
    } обьекту зомби инфекцию

entity-effect-guidebook-innoculate-zombie-infection =
    { $chance ->
        [1] Лечит
        *[other] лечит
    } текущую зомби инфекцию и даёт иммунитет к ней

entity-effect-guidebook-reduce-rotting =
    { $chance ->
        [1] Регенерирует
        *[other] регенерирует
    } {NATURALFIXED($time, 3)} {MANY("секунд", $time)} гниения

entity-effect-guidebook-area-reaction =
    { $chance ->
        [1] Вызывает
        *[other] вызывает
    } дым или пену на {NATURALFIXED($duration, 3)} {MANY("секунд", $duration)}

entity-effect-guidebook-add-to-solution-reaction =
    { $chance ->
        [1] Добавляет
        *[other] добавляет
    } {$reagent} во внутреннюю ёмкость с раствором

entity-effect-guidebook-artifact-unlock =
    { $chance ->
        [1] Помогает
        *[other] помогает
        } открыть узел артефакта

entity-effect-guidebook-artifact-durability-restore =
    Восстанавливает {$restored} прочность в данном артефакте

entity-effect-guidebook-plant-attribute =
    { $chance ->
        [1] Добавляет
        *[other] добавляет
    } {$attribute} к {$positive ->
    [true] [color=red]{$amount}[/color]
    *[false] [color=green]{$amount}[/color]
    }

entity-effect-guidebook-plant-cryoxadone =
    { $chance ->
        [1] Омолаживает
        *[other] омолаживает
    } растение в зависимости от его возраста и времени на вырост

entity-effect-guidebook-plant-phalanximine =
    { $chance ->
        [1] Восстанавливает
        *[other] Восстанавливает
    } выживаемость у растения с мутацией на ген невыживаемости

entity-effect-guidebook-plant-diethylamine =
    { $chance ->
        [1] Увеличивает
        *[other] увеличивает
    } продолжительность жизни растения и/или его базовое состояние здоровья с 10% вероятностью для каждого параметра

entity-effect-guidebook-plant-robust-harvest =
    { $chance ->
        [1] Увеличивает
        *[other] увеличивает
    } потенцию растения на {$increase} до максимума в {$limit}. Приводит к бессемянности по достижению потенции в {$seedlesstreshold}. Попытка превышения {$limit} может повлечь снижение урожайности с шансом в 10%

entity-effect-guidebook-plant-seeds-add =
    { $chance ->
        [1] Восстанавливает
        *[other] Восстанавливает
    } семянность в растении

entity-effect-guidebook-plant-seeds-remove =
    { $chance ->
        [1] Удаляет
        *[other] удаляет
    } семена с растения
