using Mouzie.Data.Models.Domain;

namespace Mouzie.Data.Repositories.Interfaces
{
    public interface IInvoiceRepository
    {
        Task<Invoice?> GetByIdAsync(int id);
        Task<Invoice?> GetByBookingIdAsync(int bookingId);
        Task<int> CreateInvoiceAsync(Invoice invoice);
        Task MarkPaidAsync(int invoiceId);
        Task SoftDeleteAsync(int invoiceId);
    }
}