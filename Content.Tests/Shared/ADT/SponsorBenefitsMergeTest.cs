using System;
using System.Collections.Generic;
using Content.Server.ADT.Sponsors;
using Content.Shared.ADT.Sponsors;
using NUnit.Framework;
using Robust.Shared.Maths;

namespace Content.Tests.Shared.ADT;

[TestFixture]
[TestOf(typeof(SponsorBenefits))]
public sealed class SponsorBenefitsMergeTest
{
    private static SponsorBenefitLayer Layer(SponsorBenefits benefits, int priority)
    {
        return new SponsorBenefitLayer(benefits, priority);
    }

    [Test]
    public void EmptyMergeGivesNothing()
    {
        var merged = SponsorBenefits.Merge(Array.Empty<SponsorBenefitLayer>());

        Assert.Multiple(() =>
        {
            Assert.That(merged.RoleBypass, Is.EqualTo(SponsorRoleBypass.None));
            Assert.That(merged.ExtraCharacterSlots, Is.Zero);
            Assert.That(merged.Loadouts, Is.Empty);
            Assert.That(merged.ExcludedDepartments, Is.Empty);
        });
    }

    [Test]
    public void BoolFlagsAreOred()
    {
        var first = new SponsorBenefits
        {
            PriorityJoin = true,
        };

        var second = new SponsorBenefits
        {
            AllowCustomOocColor = true,
        };

        var merged = SponsorBenefits.Merge(new[] { Layer(first, 0), Layer(second, 10) });

        Assert.Multiple(() =>
        {
            Assert.That(merged.PriorityJoin, Is.True);
            Assert.That(merged.AllowCustomOocColor, Is.True);
            Assert.That(merged.AllowCustomGhostColor, Is.False);
        });
    }

    [Test]
    public void ExtraSlotsTakeMaximumNotSum()
    {
        var first = new SponsorBenefits
        {
            ExtraCharacterSlots = 1,
        };

        var second = new SponsorBenefits
        {
            ExtraCharacterSlots = 3,
        };

        var merged = SponsorBenefits.Merge(new[] { Layer(first, 0), Layer(second, 10) });

        Assert.That(merged.ExtraCharacterSlots, Is.EqualTo(3));
    }

    [Test]
    public void UnlocksAreUnioned()
    {
        var tier = new SponsorBenefits();
        tier.Loadouts.Add("TierCoat");
        tier.Markings.Add("TierTail");

        var personal = new SponsorBenefits();
        personal.Loadouts.Add("NamedCoat");

        var merged = SponsorBenefits.Merge(new[] { Layer(tier, 0), Layer(personal, 1000) });

        Assert.Multiple(() =>
        {
            Assert.That(merged.Loadouts, Is.EquivalentTo(new[] { "TierCoat", "NamedCoat" }));
            Assert.That(merged.Markings, Is.EquivalentTo(new[] { "TierTail" }));
        });
    }

    [Test]
    public void HighestPriorityWinsOocColor()
    {
        var tier = new SponsorBenefits
        {
            OocColor = Color.Red,
        };

        var personal = new SponsorBenefits
        {
            OocColor = Color.Blue,
        };

        var merged = SponsorBenefits.Merge(new[] { Layer(tier, 5), Layer(personal, 1005) });

        Assert.That(merged.OocColor, Is.EqualTo(Color.Blue));
    }

    [Test]
    public void LowerPriorityColorDoesNotOverwrite()
    {
        var tier = new SponsorBenefits
        {
            OocColor = Color.Red,
        };

        var weak = new SponsorBenefits
        {
            OocColor = Color.Green,
        };

        var merged = SponsorBenefits.Merge(new[] { Layer(tier, 100), Layer(weak, 1) });

        Assert.That(merged.OocColor, Is.EqualTo(Color.Red));
    }

    [Test]
    public void RoleBypassFlagsAreOred()
    {
        var jobs = new SponsorBenefits
        {
            RoleBypass = SponsorRoleBypass.Jobs,
        };

        var antags = new SponsorBenefits
        {
            RoleBypass = SponsorRoleBypass.Antags,
        };

        var merged = SponsorBenefits.Merge(new[] { Layer(jobs, 0), Layer(antags, 1) });

        Assert.That(merged.RoleBypass, Is.EqualTo(SponsorRoleBypass.Jobs | SponsorRoleBypass.Antags));
    }

    [Test]
    public void ExclusionsAreIntersectedAcrossBypassingLayers()
    {
        var strict = new SponsorBenefits
        {
            RoleBypass = SponsorRoleBypass.Jobs,
        };
        strict.ExcludedDepartments.Add("Command");
        strict.ExcludedDepartments.Add("Security");

        var loose = new SponsorBenefits
        {
            RoleBypass = SponsorRoleBypass.Jobs,
        };
        loose.ExcludedDepartments.Add("Security");

        var merged = SponsorBenefits.Merge(new[] { Layer(strict, 0), Layer(loose, 1) });

        Assert.That(merged.ExcludedDepartments, Is.EquivalentTo(new[] { "Security" }));
    }

    [Test]
    public void UnrestrictedLayerClearsAllExclusions()
    {
        var restricted = new SponsorBenefits
        {
            RoleBypass = SponsorRoleBypass.Jobs,
        };
        restricted.ExcludedDepartments.Add("Command");

        var unrestricted = new SponsorBenefits
        {
            RoleBypass = SponsorRoleBypass.Jobs,
        };

        var merged = SponsorBenefits.Merge(new[] { Layer(restricted, 0), Layer(unrestricted, 1) });

        Assert.That(merged.ExcludedDepartments, Is.Empty);
    }

    [Test]
    public void LayerWithoutJobBypassDoesNotAffectExclusions()
    {
        var bypass = new SponsorBenefits
        {
            RoleBypass = SponsorRoleBypass.Jobs,
        };
        bypass.ExcludedDepartments.Add("Command");

        var unrelated = new SponsorBenefits
        {
            PriorityJoin = true,
        };

        var merged = SponsorBenefits.Merge(new[] { Layer(bypass, 0), Layer(unrelated, 1) });

        Assert.That(merged.ExcludedDepartments, Is.EquivalentTo(new[] { "Command" }));
    }

    [Test]
    public void GhostColorsAreDeduplicated()
    {
        var first = new SponsorBenefits();
        first.GhostColors.Add(Color.Red);
        first.GhostColors.Add(Color.Blue);

        var second = new SponsorBenefits();
        second.GhostColors.Add(Color.Blue);
        second.GhostColors.Add(Color.Green);

        var merged = SponsorBenefits.Merge(new[] { Layer(first, 0), Layer(second, 1) });

        Assert.That(merged.GhostColors, Is.EquivalentTo(new[] { Color.Red, Color.Blue, Color.Green }));
    }

    [Test]
    public void MergeDoesNotMutateSourceLayers()
    {
        var tier = new SponsorBenefits();
        tier.Loadouts.Add("TierCoat");

        var personal = new SponsorBenefits();
        personal.Loadouts.Add("NamedCoat");

        SponsorBenefits.Merge(new[] { Layer(tier, 0), Layer(personal, 1000) });

        Assert.Multiple(() =>
        {
            Assert.That(tier.Loadouts, Is.EquivalentTo(new[] { "TierCoat" }));
            Assert.That(personal.Loadouts, Is.EquivalentTo(new[] { "NamedCoat" }));
        });
    }

    [Test]
    public void ResolvedDataAnswersJobBypass()
    {
        var benefits = new SponsorBenefits
        {
            RoleBypass = SponsorRoleBypass.Jobs,
        };
        benefits.ExcludedJobs.Add("Captain");

        var data = SponsorData.FromBenefits(benefits, null, Array.Empty<SponsorTierSummary>());

        Assert.Multiple(() =>
        {
            Assert.That(data.IsJobTimeBypassed("Engineer", "Engineering"), Is.True);
            Assert.That(data.IsJobTimeBypassed("Captain", "Command"), Is.False);
            Assert.That(data.IsAntagTimeBypassed(), Is.False);
            Assert.That(data.HasAnyBenefit, Is.True);
        });
    }

    [Test]
    public void EmptyDataGrantsNothing()
    {
        Assert.Multiple(() =>
        {
            Assert.That(SponsorData.Empty.HasAnyBenefit, Is.False);
            Assert.That(SponsorData.Empty.IsLoadoutAllowed("NamedCoat"), Is.False);
            Assert.That(SponsorData.Empty.IsJobTimeBypassed("Engineer", "Engineering"), Is.False);
        });
    }

    [Test]
    public void BenefitsSurviveJsonRoundTrip()
    {
        var benefits = new SponsorBenefits
        {
            RoleBypass = SponsorRoleBypass.Jobs | SponsorRoleBypass.Antags,
            OocColor = Color.Red,
            AllowCustomGhostColor = true,
            ExtraCharacterSlots = 2,
        };
        benefits.Loadouts.Add("NamedCoat");
        benefits.ExcludedDepartments.Add("Command");
        benefits.GhostColors.Add(Color.Blue);

        var json = SponsorSerialization.SerializeBenefits(benefits);

        Assert.That(SponsorSerialization.TryDeserializeBenefits(json, out var restored), Is.True);

        Assert.Multiple(() =>
        {
            Assert.That(restored.RoleBypass, Is.EqualTo(benefits.RoleBypass));
            Assert.That(restored.OocColor, Is.EqualTo(Color.Red));
            Assert.That(restored.GhostColors, Is.EquivalentTo(new[] { Color.Blue }));
            Assert.That(restored.Loadouts, Is.EquivalentTo(new[] { "NamedCoat" }));
            Assert.That(restored.ExcludedDepartments, Is.EquivalentTo(new[] { "Command" }));
            Assert.That(restored.ExtraCharacterSlots, Is.EqualTo(2));
            Assert.That(restored.AllowCustomGhostColor, Is.True);
        });
    }

    [Test]
    public void BrokenJsonDoesNotThrow()
    {
        Assert.Multiple(() =>
        {
            Assert.That(SponsorSerialization.TryDeserializeBenefits("{ not json", out var broken), Is.False);
            Assert.That(broken.RoleBypass, Is.EqualTo(SponsorRoleBypass.None));
            Assert.That(SponsorSerialization.TryDeserializeBenefits(null, out _), Is.True);
        });
    }
}
