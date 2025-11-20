using Microsoft.EntityFrameworkCore;
using MySalesTracker.Application.Interfaces;
using MySalesTracker.Domain.Entities;
using MySalesTracker.Domain.Enums;

namespace MySalesTracker.Infrastructure.Persistence.Repositories;

internal class PaymentRepository(IDbContextFactory<AppDbContext> contextFactory) : IPaymentRepository
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory = contextFactory;

    public async Task<List<Payment>> GetPaymentsByEventDayAsync(int eventDayId, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        return await context.Payments
            .Where(p => p.EventDayId == eventDayId)
            .OrderBy(p => p.Method)
            .ToListAsync(ct);
    }

    public async Task<Payment> UpsertPaymentAsync(int eventDayId, PaymentMethod method, decimal amount, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        var existingPayment = await context.Payments
            .FirstOrDefaultAsync(p => p.EventDayId == eventDayId && p.Method == method, ct);

        if (existingPayment is not null)
        {
            var updatedPayment = new Payment
            {
                PaymentId = existingPayment.PaymentId,
                EventDayId = eventDayId,
                Method = method,
                Amount = amount
            };
            
            context.Entry(existingPayment).CurrentValues.SetValues(updatedPayment);
            await context.SaveChangesAsync(ct);
            return updatedPayment;
        }

        // Insert new payment
        var newPayment = new Payment
        {
            EventDayId = eventDayId,
            Method = method,
            Amount = amount
        };

        context.Payments.Add(newPayment);
        await context.SaveChangesAsync(ct);

        return newPayment;
    }

    public async Task DeletePaymentAsync(int paymentId, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        var payment = await context.Payments.FindAsync([paymentId], ct);
        if (payment is not null)
        {
            context.Payments.Remove(payment);
            await context.SaveChangesAsync(ct);
        }
    }
}

