## Консоли

ent-ComputerCargoOrdersScience = Консоль заказов научного отдела
 .desc = Какая-то научная хуйня для заказа более важной научной хуйни

ent-ComputerCargoOrdersMedical = Консоль заказов медицинского отдела
  .desc = Какая-то медицинская хуйня для заказа более важной научной хуйни

ent-ComputerCargoOrdersSecurity = Консоль заказаов службы безопастности
  .desc = Какая-то медицинская хуйня для заказа более важной научной хуйни

ent-ComputerCargoOrdersService = Консоль заказов сервисного отдела
  .desc = Какая-то медицинская хуйня для заказа более важной научной хуйни

ent-ComputerCargoOrdersEngineering = Консоль заказов инженерного отдела
  .desc = Какая-то медицинская хуйня для заказа более важной научной хуйни

## Платы

ent-CargoRequestScienceComputerCircuitboard = Консоль заказов научного отдела (машинная плата)
 .desc = Плата для какой-то хуйни

ent-CargoRequestMedicalComputerCircuitboard = Консоль заказов медицинского отдела (машинная плата)
 .desc = Плата для какой-то хуйни

ent-CargoRequestEngineeringComputerCircuitboard = Консоль заказаов службы безопастности (машинная плата)
 .desc = Плата для какой-то хуйни

ent-CargoRequestSecurityComputerCircuitboard = Консоль заказов сервисного отдела (машинная плата)
 .desc = Плата для какой-то хуйни

ent-CargoRequestServiceComputerCircuitboard = Консоль заказов инженерного отдела (машинная плата)
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
