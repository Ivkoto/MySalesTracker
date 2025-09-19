namespace MySalesTracker.Data.Models;
public class Event
{
    public int EventId { get; set; }
    public string Name { get; set; } = null!;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public ICollection<EventDay> Days { get; set; } = [];
}

public class EventDay
{
    public int EventDayId { get; set; }
    public int EventId { get; set; }
    public Event Event { get; set; } = null!;
    public DateOnly Date { get; set; }
    public ICollection<Sale> Sales { get; set; } = [];
    public ICollection<Expense> Expenses { get; set; } = [];
    public ICollection<Payment> PaymentsCounted { get; set; } = [];
}