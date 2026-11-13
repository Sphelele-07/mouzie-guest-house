namespace Mouzie.UI.Services;

public record EmailAttachment(string FileName, byte[] Content, string ContentType = "application/pdf");

public interface IEmailService
{
    Task SendAsync(string to, string toName, string subject, string htmlBody,
                   IEnumerable<EmailAttachment>? attachments = null,
                   CancellationToken ct = default);

    // Concrete helpers used by controllers
    Task SendBookingReceivedAsync(string guestEmail, string guestName, string bookingCode,
                                  DateTime bookingDate, string roomNumber, decimal price);
    Task SendBookingApprovedAsync(string guestEmail, string guestName, string bookingCode,
                                  byte[] invoicePdf, string invoiceNumber);
    Task SendThankYouAsync(string guestEmail, string guestName, string bookingCode);
    Task NotifyAdminNewBookingAsync(string bookingCode, string guestName, string guestPhone,
                                    string guestIdNumber, DateTime bookingDate, string roomNumber);
}