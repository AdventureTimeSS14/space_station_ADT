// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.Changeling;
using Content.Shared.Cloning.Events; // ADT-Tweak
using Content.Shared.Examine;
using Content.Shared.Mobs;
using Content.Shared.Traits.Assorted; // ADT-Tweak

namespace Content.Goobstation.Shared.Changeling.Systems;

public sealed partial class AbsorbedSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AbsorbedComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<AbsorbedComponent, MobStateChangedEvent>(OnMobStateChange);
        SubscribeLocalEvent<AbsorbedComponent, CloningEvent>(OnCloned); // ADT-Tweak
    }

    // ADT-Tweak start
    /// <summary>
    /// Снимает <see cref="UnrevivableComponent"/> с клона только для тел, опустошённых генокрадом
    /// (причина дефиба "defibrillator-hollow"), чтобы не затронуть трейт Unrevivable.
    /// </summary>
    private void OnCloned(Entity<AbsorbedComponent> ent, ref CloningEvent args)
    {
        if (TryComp<UnrevivableComponent>(ent, out var unrevivable)
            && unrevivable.ReasonMessage.Id == "defibrillator-hollow")
            RemComp<UnrevivableComponent>(args.CloneUid);
    }
    // ADT-Tweak end

    private void OnExamine(Entity<AbsorbedComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("changeling-absorb-onexamine"));
    }

    private void OnMobStateChange(Entity<AbsorbedComponent> ent, ref MobStateChangedEvent args)
    {
        // in case one somehow manages to dehusk someone
        if (args.NewMobState != MobState.Dead)
            RemComp<AbsorbedComponent>(ent);
    }
}