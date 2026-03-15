cmd-languageadd-desc = Adds a language to an entity.
cmd-languageadd-help = Usage: languageadd <entity uid> <language|all> [knowledge]
cmd-languageadd-success = Language { $language } ({ $knowledge }) added to entity { $entity }.
cmd-languageadd-all-success = Added { $count } languages to entity { $entity } with knowledge { $knowledge }.
cmd-languageadd-invalid-language = Language { $language } not found.
cmd-languageadd-invalid-knowledge = Invalid language knowledge level. Use: Understand, BadSpeak, Speak.
cmd-languageadd-no-language-component = Entity does not have language component.

cmd-languageslist-desc = Shows the list of languages an entity knows.
cmd-languageslist-help = Usage: languageslist <entity uid>
cmd-languageslist-header = Languages of entity { $entity }:
cmd-languageslist-empty = Entity does not know any languages.
cmd-languageslist-no-language-component = Entity { $entity } does not have language component.
cmd-languageslist-line =  - { $proto } ({ $id }): { $knowledge }{ $current }
cmd-languageslist-current-suffix =  [CURRENT]

cmd-languageremove-desc = Removes a language from an entity.
cmd-languageremove-help = Usage: languageremove <entity uid> <language|all>
cmd-languageremove-success = Language { $language } removed from entity { $entity }.
cmd-languageremove-all-success = Removed { $count } languages from entity { $entity }.
cmd-languageremove-invalid-language = Language { $language } not found.
cmd-languageremove-no-language-component = Entity { $entity } does not have language component.
cmd-languageremove-not-known = Entity { $entity } does not know language { $language }.

cmd-language-hint = Language
cmd-knowledge-hint = Knowledge
