
ent-ComputerCargoOrdersScience = консоль заказов научного отдела
 .desc = Используется научным отделом для заказа снабжения

ent-ComputerCargoOrdersMedical = консоль заказов медицинского отдела
  .desc = Используется медицинским отделом для заказа снабжения

ent-ComputerCargoOrdersSecurity = консоль заказов службы безопасности
  .desc = Используется службой безопасности для заказа снабжения

ent-ComputerCargoOrdersService = консоль заказов сервисного отдела
  .desc = Используется сервисным отделом для заказа снабжения

ent-ComputerCargoOrdersEngineering = консоль заказов инженерного отдела
  .desc = Используется инженерным отделом для заказа снабжения

ent-CargoRequestScienceComputerCircuitboard = консоль заказов научного отдела (консольная плата)
 .desc = Консольная плата для консоли заказов научного отдела

ent-CargoRequestMedicalComputerCircuitboard = консоль заказов медицинского отдела (консольная плата)
 .desc = Консольная плата для консоли заказов медицинского отдела

ent-CargoRequestServiceComputerCircuitboard = консоль заказов сервисного отдела (консольная плата)
 .desc = Консольная плата для консоли заказов сервисного отдела

ent-CargoRequestSecurityComputerCircuitboard = консоль заказов службы безопасности (консольная плата)
 .desc = Консольная плата для консоли заказов службы безопасности

ent-CargoRequestEngineeringComputerCircuitboard = консоль заказов инженерного отдела (консольная плата)
 .desc = Консольная плата для консоли заказов инженерного отдела


cargo-acquisition-slip-body = [head=3]Информация[/head]
    {"[bold]Товар:[/bold]"} {$product}
    {"[bold]Описание:[/bold]"} {$description}
    {"[bold]Цена за штуку:[/bold"}] ${$unit}
    {"[bold]Количество:[/bold]"} {$amount}
    {"[bold]Итоговая стоимость:[/bold]"} ${$cost}

    {"[head=3]Детали покупки[/head]"}
    {"[bold]Заказчик:[/bold]"} {$orderer}
    {"[bold]Причина:[/bold]"} {$reason}








ent-PaperAcquisitionSlipEngineering = квитанция о приобретении
 .desc = Квитанция с данными заказа. Её можно передать в отдел снабжения для завершения заказа.
 .suffix = Инженерный

ent-PaperAcquisitionSlipService = квитанция о приобретении
 .desc = Квитанция с данными заказа. Её можно передать в отдел снабжения для завершения заказа.
 .suffix = Сервисный

ent-PaperAcquisitionSlipSecurity = квитанция о приобретении
 .desc = Квитанция с данными заказа. Её можно передать в отдел снабжения для завершения заказа.
 .suffix = Служба безопасности

ent-PaperAcquisitionSlipScience = квитанция о приобретении
 .desc = Квитанция с данными заказа. Её можно передать в отдел снабжения для завершения заказа.
 .suffix = Научный

ent-PaperAcquisitionSlipMedical = квитанция о приобретении
 .desc = Квитанция с данными заказа. Её можно передать в отдел снабжения для завершения заказа.
 .suffix = Медицинский