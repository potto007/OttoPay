using OttoPay.Banking;

namespace OttoPay.Tests;

public class DepositHandlersTests
{
    private static readonly Inventory Drag = Items.Grid(1, 1, "drag");
    private static readonly ItemDrop.ItemData Ruby = Items.Stack("$item_ruby", 3, 50);

    private static void Add(DepositHandlers handlers, string name, Func<bool> canDeposit, Func<bool>? deposit = null, string description = "")
    {
        handlers.Register(name, (_, _, _) => canDeposit(), (_, _, _) => (deposit ?? (() => true))(), (_, _, _) => description);
    }

    [Fact]
    public void No_handler_takes_a_drag_nobody_registered_for()
    {
        Assert.Null(new DepositHandlers().Accepting(Drag, Ruby, 3));
    }

    [Fact]
    public void The_first_handler_that_accepts_takes_the_drag()
    {
        DepositHandlers handlers = new();
        Add(handlers, "Refuses", () => false);
        Add(handlers, "First", () => true);
        Add(handlers, "Second", () => true);

        Assert.Equal("First", handlers.Accepting(Drag, Ruby, 3)?.Name);
    }

    [Fact]
    public void Registering_a_name_again_replaces_it_in_place()
    {
        DepositHandlers handlers = new();
        Add(handlers, "A", () => false);
        Add(handlers, "B", () => true, description: "old");
        Add(handlers, "A", () => true, description: "new");

        DepositHandler? handler = handlers.Accepting(Drag, Ruby, 3);

        Assert.Equal("A", handler?.Name);
        Assert.Equal("new", handler?.Describe(Drag, Ruby, 3));
    }

    [Fact]
    public void A_handler_that_throws_counts_as_a_refusal()
    {
        DepositHandlers handlers = new();
        Add(handlers, "Broken", () => throw new InvalidOperationException("boom"));
        Add(handlers, "Fine", () => true);

        Assert.Equal("Fine", handlers.Accepting(Drag, Ruby, 3)?.Name);
    }

    [Fact]
    public void A_deposit_that_throws_reports_failure()
    {
        DepositHandlers handlers = new();
        Add(handlers, "Broken", () => true, () => throw new InvalidOperationException("boom"));

        DepositHandler handler = handlers.Accepting(Drag, Ruby, 3)!;

        Assert.False(handler.Deposit(Drag, Ruby, 3));
    }

    [Fact]
    public void A_handler_may_unregister_itself_while_being_asked()
    {
        DepositHandlers handlers = new();
        Add(handlers, "Leaving", () =>
        {
            handlers.Unregister("Leaving");
            return false;
        });
        Add(handlers, "Staying", () => true);

        Assert.Equal("Staying", handlers.Accepting(Drag, Ruby, 3)?.Name);
        Assert.Equal("Staying", handlers.Accepting(Drag, Ruby, 3)?.Name);
    }
}
