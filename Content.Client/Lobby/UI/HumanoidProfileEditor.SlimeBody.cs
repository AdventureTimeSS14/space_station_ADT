// SPDX-FileCopyrightText: 2026 ultradyper
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Collections.Generic;
using Content.Shared.ADT.SlimeBody;

namespace Content.Client.Lobby.UI;

public sealed partial class HumanoidProfileEditor
{
    /// <summary>
    /// Refreshes the slime body composition dropdown: visible only for slime people.
    /// </summary>
    public void RefreshSlimeBody()
    {
        if (Profile == null)
            return;

        var isSlime = Profile.Species == "SlimePerson";
        SlimeBodyContainer.Visible = isSlime;

        // Rebuild items: regular slime (index 0) then every composition in order.
        SlimeBodyButton.Clear();
        SlimeBodyButton.AddItem(Loc.GetString("humanoid-profile-editor-slime-body-none"), 0);
        var index = 1;
        foreach (var composition in SlimeBodyCompositions.All)
        {
            SlimeBodyButton.AddItem(Loc.GetString(composition.Name), index);
            index++;
        }

        var current = Profile.SlimeBodyComposition;
        var currentIndex = 0;
        if (current != null)
        {
            var i = 1;
            foreach (var composition in SlimeBodyCompositions.All)
            {
                if (composition.Id == current)
                {
                    currentIndex = i;
                    break;
                }
                i++;
            }
        }

        SlimeBodyButton.SelectId(currentIndex);
    }

    public void SelectSlimeBody(int id)
    {
        if (Profile == null)
            return;

        string? compositionId = null;
        var i = 1;
        foreach (var composition in SlimeBodyCompositions.All)
        {
            if (i == id)
            {
                compositionId = composition.Id;
                break;
            }
            i++;
        }

        Profile = Profile.WithSlimeBodyComposition(compositionId);
        SetDirty();
        RefreshSlimeBody();
    }
}
