eldritch-id-card-component-examine-inverted = Current effect is [color=yellow]inverted[/color]

eldritch-id-card-component-examine-message =
    Enchanted by the Mansus!
    Using an ID on this or using this ID on another ID will consume it and allow you to copy its accesses.
    Using this on a pair of doors, allows you to link them together. Entering one door will transport you to the other, while heathens are instead teleported to a random airlock.
    Alt-clicking the ID, makes the ID make inverted portals instead, which teleport you onto a random airlock onstation, while heathens are teleported to the destination.

eldritch-id-card-component-on-invert =
    { $inverted ->
      [true] now
      *[false] no longer
    } creating inverted rifts

eldritch-id-card-component-portal-inverted =
    portal { $inverted ->
             [true] inverted
             *[false] no longer inverted
           }

eldritch-id-card-component-invert = Invert
eldritch-id-card-component-invert-message = Make the ID make inverted portals, which teleport you onto a random airlock onstation, while heathens are teleported to the destination or vise versa.

eldritch-id-card-component-link-one = link 1/2
eldritch-id-card-component-link-two = link 2/2

lock-portal-component-clear-portals = Clear both links

lock-portal-component-examine-inverted = [color=yellow]inverted[/color]
lock-portal-component-examine-not-inverted = [color=yellow]not inverted[/color]

lock-portal-component-examine-message =
    Portal is {$status}.
    Click it using eldritch id to invert it.
    Alt-click with eldrith id to remove both links.
