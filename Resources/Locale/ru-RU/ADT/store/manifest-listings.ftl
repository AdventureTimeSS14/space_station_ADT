manifest-listing-entry-start = [color=gray]Потрачено:[/color] {$spent}
manifest-listing-entry-listing = [font size=30]\[[tex path="{$sprite}" state="{$state}" offsetY=-12 tooltip="{$info}"]{$amount ->
    [1] {""}
    *[other] x{$amount}
}\][/font]
manifest-listing-entry-info = {$name} - {$spent}
manifest-listing-entry-unknown = Неизвестный товар
manifest-listing-currency = {$amount} {$currency}
manifest-listing-free = бесплатно
