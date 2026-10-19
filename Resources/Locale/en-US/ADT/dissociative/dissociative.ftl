ent-ActionDissociativeManifest = Manifest
    .desc = Step out of your host or go back inside. Only the host can see you outside.
ent-ActionDissociativeFront = Take control
    .desc = Take over the host's body for a while.
ent-ActionDissociativeRelease = Give control back
    .desc = Return the body to its host early.
ent-ActionDissociativeSetup = Shape personality
    .desc = Pick a name, sex, voice and look. Available once.

dissociative-not-in-control = You are not in control of the body right now.
dissociative-manifest-host-contained = Your host is shut in, you can't get out.
dissociative-front-cooldown = You are not strong enough yet. About { $minutes } min left.
dissociative-front-suppressed = The pills muffle you, you can't break through.
dissociative-front-mindshield = Something in the host's head keeps you from surfacing.
dissociative-front-host-incapacitated = The host's body is incapacitated.
dissociative-front-start-alter = The body is yours.
dissociative-front-start-host = You are losing control of your body...
dissociative-front-end-host = You are in control again. The last moments are a blur.
dissociative-front-end-alter = You are pulled back in.
dissociative-examine-fronting = [color=#b81500]Their gaze feels foreign.[/color]
dissociative-setup-done = Personality shaped.

dissociative-setup-window-title = Second personality
dissociative-setup-window-info = Only your host sees your name and sex. You can set this once.
dissociative-setup-window-name = Name
dissociative-setup-window-sex = Sex
dissociative-setup-window-customize = Look and voice
dissociative-setup-window-confirm = Confirm
dissociative-setup-sex-male = Male
dissociative-setup-sex-female = Female
dissociative-setup-sex-unsexed = None

ghost-role-information-dissociative-alter-name = Second personality ({ $host })
ghost-role-information-dissociative-alter-description = You are a second personality living in someone else's head. Talk to your host, step outside and sometimes take the body over.
ghost-role-information-dissociative-alter-rules =
    You are the [color={ role-type-familiar-color }][bold]second personality[/bold][/color] of a character. Only your host can see and hear you.
    You are not an antagonist unless you are given an antagonist role. While in control of the body you follow the same rules as any crewmember.
    You don't remember any of your previous life, and you don't remember anything you learned as a ghost.

reagent-effect-guidebook-antipsychotic =
    { $chance ->
        [1] Suppresses
       *[other] suppress
    } a second personality

ent-ActionDissociativeSurrender = Stop fighting
    .desc = Give your body to the second personality for good and become the voice inside yourself.
dissociative-surrender-confirm = Press again to give up your body for good.
dissociative-surrender-no-alter = There is no one inside to give in to.
dissociative-surrender-alter = The host gave in. The body is yours for good.
dissociative-surrender-host = You stop resisting and dissolve inside.

ent-ActionMidroundCustomization = Change appearance
    .desc = Change your hair, facial hair and voice.

ent-ActionDissociativeGiveControl = Give up the body
    .desc = Willingly hand your body to the second personality for the usual time.
dissociative-give-control-confirm = Press again to hand your body to the second personality.
dissociative-give-control-no-alter = The second personality cannot take the body right now.

reagent-effect-guidebook-dissociative-extend =
    { $chance ->
        [1] Extends
       *[other] extend
    } a second personality's time in the body by { $seconds }s
reagent-effect-guidebook-dissociative-force =
    { $chance ->
        [1] Lets
       *[other] let
    } a second personality take over the body
