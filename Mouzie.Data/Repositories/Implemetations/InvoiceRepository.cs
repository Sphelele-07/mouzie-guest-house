using Mouzie.Data.DataAccess;
using Mouzie.Data.Models.Domain;
using Mouzie.Data.Repositories.Interfaces;

namespace Mouzie.Data.Repositories.Implementations;

public class InvoiceRepository : IInvoiceRepository
{
    private readonly ISqlDataAccess _db;
    public InvoiceRepository(ISqlDataAccess db) => _db = db;

    public async Task<Invoice?> GetByIdAsync(int id)
    {
        var inv = await _db.QuerySingleAsync<Invoice>("usp_Invoice_GetById", new { InvoiceId = id });
        if (inv is null) return null;
        inv.Items = (await _db.QueryAsync<InvoiceItem>("usp_InvoiceItem_GetByInvoiceId",
            new { InvoiceId = id })).ToList();
        return inv;
    }

    public async Task<Invoice?> GetByBookingIdAsync(int bookingId)
    {
        var inv = await _db.QuerySingleAsync<Invoice>("usp_Invoice_GetByBookingId",
            new { BookingId = bookingId });
        if (inv is null) return null;
        inv.Items = (await _db.QueryAsync<InvoiceItem>("usp_InvoiceItem_GetByInvoiceId",
            new { InvoiceId = inv.InvoiceId })).ToList();
        return inv;
    }

    public async Task<int> CreateInvoiceAsync(Invoice inv)
    {
        var id = await _db.ExecuteScalarAsync<int>("usp_Invoice_Insert", new
        {
            inv.BookingId,
            inv.InvoiceNumber,
            inv.IssueDate,
            inv.DueDate,
            inv.Status,
            inv.TotalAmount
        });

        foreach (var item in inv.Items)
        {
            await _db.ExecuteAsync("usp_InvoiceItem_Insert", new
            {
                InvoiceId = id,
                item.Description,
                item.Amount
            });
        }
        return id;
    }

    public async Task MarkPaidAsync(int invoiceId)
        => await _db.ExecuteAsync("usp_Invoice_MarkPaid", new { InvoiceId = invoiceId });

    public async Task SoftDeleteAsync(int invoiceId)
        => await _db.ExecuteAsync("usp_Invoice_SoftDelete", new { InvoiceId = invoiceId });
}