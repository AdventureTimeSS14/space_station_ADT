using System.Collections.Generic;
using System.Linq;
using Content.Client.ADT.Actions;
using NUnit.Framework;
using Robust.Shared.Prototypes;

namespace Content.Tests.Client.ADT;

[TestFixture]
[TestOf(typeof(ADTActionOrder))]
public sealed class ADTActionOrderTest
{
    private static readonly EntProtoId A = "ActionA";
    private static readonly EntProtoId B = "ActionB";
    private static readonly EntProtoId C = "ActionC";
    private static readonly EntProtoId D = "ActionD";

    private static List<EntProtoId?> Hotbar(params EntProtoId[] actions)
    {
        return actions.Select(action => (EntProtoId?) action).ToList();
    }

    [Test]
    public void StoreRemembersHotbarOrder()
    {
        var order = new ADTActionOrder();

        Assert.That(order.Store(Hotbar(B, A, C)), Is.True);
        Assert.That(order.Order, Is.EqualTo(new[] { B, A, C }));
        Assert.That(order.Store(Hotbar(B, A, C)), Is.False);
    }

    [Test]
    public void MissingActionsKeepTheirPlaces()
    {
        var order = new ADTActionOrder();
        order.Store(Hotbar(A, B, C, D));

        order.Store(Hotbar(C, A, D));

        Assert.That(order.Order, Is.EqualTo(new[] { C, B, A, D }));
    }

    [Test]
    public void PartialHotbarDoesNotBreakOrder()
    {
        var order = new ADTActionOrder();
        order.Store(Hotbar(A, B, C, D));

        order.Store(Hotbar(D));
        order.Store(Hotbar(B, D));
        order.Store(Hotbar(A, B, D));

        Assert.That(order.Order, Is.EqualTo(new[] { A, B, C, D }));
    }

    [Test]
    public void EmptyAndUnknownEntriesAreIgnored()
    {
        var order = new ADTActionOrder();
        order.Store(new List<EntProtoId?> { A, null, B, A });

        Assert.That(order.Order, Is.EqualTo(new[] { A, B }));
    }

    [Test]
    public void ArrangeUsesRememberedOrder()
    {
        var order = new ADTActionOrder();
        order.Store(Hotbar(C, A, B));

        var arranged = order.Arrange(new[] { "a", "new", "b", "c" }, Key);

        Assert.That(arranged, Is.EqualTo(new[] { "c", "a", "b", "new" }));
    }

    [Test]
    public void ArrangeSkipsRemovedActions()
    {
        var order = new ADTActionOrder();
        order.Store(Hotbar(A, B, C));
        order.SetRemoved(B, true);

        var arranged = order.Arrange(new[] { "a", "b", "c" }, Key);

        Assert.That(arranged, Is.EqualTo(new[] { "a", "c" }));
    }

    [Test]
    public void ActionOnHotbarIsNotRemoved()
    {
        var order = new ADTActionOrder();
        order.SetRemoved(A, true);

        order.Store(Hotbar(A));

        Assert.That(order.IsRemoved(A), Is.False);
    }

    [Test]
    public void SetRemovedReportsChanges()
    {
        var order = new ADTActionOrder();

        Assert.Multiple(() =>
        {
            Assert.That(order.SetRemoved(A, true), Is.True);
            Assert.That(order.SetRemoved(A, true), Is.False);
            Assert.That(order.SetRemoved(A, false), Is.True);
            Assert.That(order.SetRemoved(A, false), Is.False);
        });
    }

    [Test]
    public void InsertIndexFollowsRememberedOrder()
    {
        var order = new ADTActionOrder();
        order.Store(Hotbar(A, B, C, D));

        Assert.Multiple(() =>
        {
            Assert.That(order.GetInsertIndex(Hotbar(A, C, D), B), Is.EqualTo(1));
            Assert.That(order.GetInsertIndex(Hotbar(B, C, D), A), Is.EqualTo(0));
            Assert.That(order.GetInsertIndex(Hotbar(A, B, C), D), Is.EqualTo(3));
            Assert.That(order.GetInsertIndex(new List<EntProtoId?> { A, null }, B), Is.EqualTo(1));
        });
    }

    [Test]
    public void LoadRestoresSavedState()
    {
        var saved = new ADTActionOrder();
        saved.Store(Hotbar(D, B, A));
        saved.SetRemoved(C, true);

        var loaded = new ADTActionOrder();
        loaded.Load(saved.Order, saved.Removed);

        Assert.Multiple(() =>
        {
            Assert.That(loaded.Order, Is.EqualTo(saved.Order));
            Assert.That(loaded.IsRemoved(C), Is.True);
            Assert.That(loaded.HasPlace(A), Is.True);
            Assert.That(loaded.HasPlace(C), Is.False);
        });
    }

    [Test]
    public void OrderIsLimited()
    {
        var order = new ADTActionOrder();
        var many = Enumerable.Range(0, ADTActionOrder.MaxEntries + 10)
            .Select(i => (EntProtoId?) new EntProtoId($"Action{i}"))
            .ToList();

        order.Store(many);

        Assert.That(order.Order, Has.Count.EqualTo(ADTActionOrder.MaxEntries));
    }

    private static EntProtoId? Key(string action)
    {
        return action switch
        {
            "a" => A,
            "b" => B,
            "c" => C,
            _ => (EntProtoId?) null,
        };
    }
}
