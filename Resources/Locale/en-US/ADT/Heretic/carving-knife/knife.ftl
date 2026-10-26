# SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
# SPDX-FileCopyrightText: 2025 Aviu00 <93730715+Aviu00@users.noreply.github.com>
#
# SPDX-License-Identifier: AGPL-3.0-or-later

carving-knife-comp-cant-draw = Can't draw carvings here!
carving-knife-comp-too-many-runes = Too many carvings!
carving-knife-comp-close-to-another-carving = Too close to another carving!
carving-knife-comp-runes-count = [color=yellow][bold]{$count} / 3[/bold] total carvings have been drawn.[/color]
carving-knife-comp-runes-deleted = Destroyed all carvings!

alert-carving-trigger-message =
    "{$victim}" has stepped foot on the alert rune near "{$location}"!
    {" "}[button label="Teleport" timer={$timer} id="{$id}" uid={$uid} coords ="{$coords}"]
    {" "}
alert-carving-trigger-message-coords = {$uid}, {$x}, {$y}

# ADT: rune descriptions for the carving knife radial menu (RuneCarvingPrototype.desc).
rune-carving-grasping-desc = Grasping Carving. When stepped on, causes heavy leg damage and stuns the victim for 5 seconds. Has 1 charge.
rune-carving-mad-desc = Mad Carving. When stepped on, causes heavy stamina damage, blinds and mutes the victim. Has 2 charges.
rune-carving-alert-desc = Alert Carving. A nearly invisible rune that alerts the carver who triggered it and where, and allows them to teleport to its location.
