using Mouzie.Data.Models.Domain;

namespace Mouzie.UI.Services;

public interface IInvoiceService
{
    /// <summary>Generate a PDF invoice for the given booking + invoice record.</summary>
    byte[] GenerateInvoicePdf(Invoice invoice, Booking booking, IReadOnlyDictionary<string, string> settings);

    /// <summary>Generate the daily check-in/out log PDF for the owner.</summary>
    byte[] GenerateDailyLogPdf(DateTime date, IEnumerable<Booking> bookings);

    byte[] GenerateDamageInvoicePdf(DamageRecord damage, Booking booking, IReadOnlyDictionary<string, string> settings);
}