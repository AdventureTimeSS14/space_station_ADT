## Консоли

ent-ComputerCargoOrdersScience = Какая-то научная хуйня
 .desc = Какая-то научная хуйня для заказа более важной научной хуйни

ent-ComputerCargoOrdersMedical = Какая-то медицинская хуйня
  .desc = Какая-то медицинская хуйня для заказа более важной научной хуйни

ent-ComputerCargoOrdersSecurity = Какая-то медицинская хуйня
  .desc = Какая-то медицинская хуйня для заказа более важной научной хуйни

ent-ComputerCargoOrdersService = Какая-то медицинская хуйня
  .desc = Какая-то медицинская хуйня для заказа более важной научной хуйни

ent-ComputerCargoOrdersEngineering = Какая-то медицинская хуйня
  .desc = Какая-то медицинская хуйня для заказа более важной научной хуйни

## Платы

ent-CargoRequestScienceComputerCircuitboard = Какая-то хуйня (машинная плата)
 .desc = Плата для какой-то хуйни

ent-CargoRequestMedicalComputerCircuitboard = Какая-то хуйня (машинная плата)
 .desc = Плата для какой-то хуйни

ent-CargoRequestEngineeringComputerCircuitboard = Какая-то хуйня (машинная плата)
 .desc = Плата для какой-то хуйни

ent-CargoRequestSecurityComputerCircuitboard = Какая-то хуйня (машинная плата)
 .desc = Плата для какой-то хуйни

ent-CargoRequestServiceComputerCircuitboard = Какая-то хуйня (машинная плата)
 .desc = Плата для какой-то хуйни





# Slip template
cargo-acquisition-slip-body = [head=3]Asset Detail[/head]
    {"[bold]Товар:[/bold]"} {$product}
    {"[bold]Описание:[/bold]"} {$description}
    {"[bold]Цена за штуку:[/bold"}] ${$unit}
    {"[bold]Количество:[/bold]"} {$amount}
    {"[bold]Итоговая стоимость:[/bold]"} ${$cost}

    {"[head=3]Детали покупки[/head]"}
    {"[bold]Заказчик:[/bold]"} {$orderer}
    {"[bold]Причина:[/bold]"} {$reason}
