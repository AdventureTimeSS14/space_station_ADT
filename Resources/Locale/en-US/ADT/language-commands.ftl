cmd-addlanguage-desc = Adds a language to an entity.
cmd-addlanguage-help = Usage: addlanguage <entity uid> <language> [knowledge]
cmd-addlanguage-success = Language { $language } ({ $knowledge }) added to entity { $entity }.
cmd-addlanguage-invalid-language = Language { $language } not found.
cmd-addlanguage-invalid-knowledge = Invalid language knowledge level. Use: Understand, BadSpeak, Speak.
cmd-addlanguage-no-language-component = Entity does not have language component.

cmd-listlanguages-desc = Shows the list of languages an entity knows.
cmd-listlanguages-help = Usage: listlanguages <entity uid>
cmd-listlanguages-header = Languages of entity { $entity }:
cmd-listlanguages-empty = Entity does not know any languages.
cmd-listlanguages-no-language-component = Entity { $entity } does not have language component.

cmd-removelanguage-desc = Removes a language from an entity.
cmd-removelanguage-help = Usage: removelanguage <entity uid> <language>
cmd-removelanguage-success = Language { $language } removed from entity { $entity }.
cmd-removelanguage-invalid-language = Language { $language } not found.
cmd-removelanguage-no-language-component = Entity { $entity } does not have language component.
cmd-removelanguage-not-known = Entity { $entity } does not know language { $language }.
