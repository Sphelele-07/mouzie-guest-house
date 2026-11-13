using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Mouzie.UI.Services;

public class SmtpOptions
{
    public string Host { get; set; } = "smtp.gmail.com";
    public int Port { get; set; } = 587;
    public bool UseStartTls { get; set; } = true;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromName { get; set; } = "Mouzie Guest House";
    public string FromEmail { get; set; } = string.Empty;
}

public class SiteOptions
{
    public string PublicBaseUrl { get; set; } = "";
    public string AdminEmail { get; set; } = "";
}

public class EmailService : IEmailService
{
    private readonly SmtpOptions _smtp;
    private readonly SiteOptions _site;
    private readonly ILogger<EmailService> _log;

    public EmailService(IOptions<SmtpOptions> smtp, IOptions<SiteOptions> site, ILogger<EmailService> log)
    {
        _smtp = smtp.Value;
        _site = site.Value;
        _log = log;
    }

    public async Task SendAsync(string to, string toName, string subject, string htmlBody,
                                IEnumerable<EmailAttachment>? attachments = null,
                                CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_smtp.Password) || _smtp.Password.StartsWith("REPLACE"))
        {
            _log.LogWarning("SMTP password is not configured. Email to {To} skipped.", to);
            return;
        }

        var msg = new MimeMessage();
        msg.From.Add(new MailboxAddress(_smtp.FromName, _smtp.FromEmail));
        msg.To.Add(new MailboxAddress(toName, to));
        msg.Subject = subject;

        var builder = new BodyBuilder { HtmlBody = WrapHtml(htmlBody) };

        if (attachments is not null)
        {
            foreach (var a in attachments)
                builder.Attachments.Add(a.FileName, a.Content, ContentType.Parse(a.ContentType));
        }

        msg.Body = builder.ToMessageBody();

        using var client = new SmtpClient();
        var socketOptions = _smtp.UseStartTls
            ? SecureSocketOptions.StartTls
            : SecureSocketOptions.SslOnConnect;

        await client.ConnectAsync(_smtp.Host, _smtp.Port, socketOptions, ct);
        await client.AuthenticateAsync(_smtp.Username, _smtp.Password, ct);
        await client.SendAsync(msg, ct);
        await client.DisconnectAsync(true, ct);

        _log.LogInformation("Email sent to {To} — subject: {Subject}", to, subject);
    }

    // ── Booking received ─────────────────────────────────────────
    public Task SendBookingReceivedAsync(string guestEmail, string guestName, string bookingCode,
                                        DateTime bookingDate, string roomNumber, decimal price)
    {
        var subject = $"Booking received — {bookingCode}";
        var body = $@"
<p>Hi {guestName},</p>
<p>Thank you for your booking request at <strong>Mouzie Guest House</strong>.</p>
<table style='border-collapse:collapse;margin:12px 0'>
  <tr><td style='padding:4px 12px 4px 0'><strong>Booking code</strong></td><td>{bookingCode}</td></tr>
  <tr><td style='padding:4px 12px 4px 0'><strong>Room</strong></td><td>Room {roomNumber}</td></tr>
  <tr><td style='padding:4px 12px 4px 0'><strong>Date</strong></td><td>{bookingDate:dd MMM yyyy}</td></tr>
  <tr><td style='padding:4px 12px 4px 0'><strong>Rate</strong></td><td>R{price:0.00}</td></tr>
</table>
<p>Ta Shoes will review and confirm your booking shortly. Once approved, you'll receive an invoice by email.</p>
<p>If you have any questions, just reply to this email or WhatsApp us.</p>
<p>— Mouzie Guest House</p>";
        return SendAsync(guestEmail, guestName, subject, body);
    }

    // ── Booking approved + invoice PDF ───────────────────────────
    public Task SendBookingApprovedAsync(string guestEmail, string guestName, string bookingCode,
                                         byte[] invoicePdf, string invoiceNumber)
    {
        var subject = $"Booking confirmed — {bookingCode}";
        var body = $@"
<p>Hi {guestName},</p>
<p>Great news — your booking <strong>{bookingCode}</strong> has been confirmed.</p>
<p>Your invoice <strong>{invoiceNumber}</strong> is attached to this email. Payment is via EFT using the
banking details on the invoice. Please use your booking code as the payment reference.</p>
<p>Check-in from 14:00. Check-out by 10:00.</p>
<p>— Mouzie Guest House</p>";

        return SendAsync(guestEmail, guestName, subject, body, new[]
        {
            new EmailAttachment($"{invoiceNumber}.pdf", invoicePdf)
        });
    }

    // ── Thank-you after checkout ─────────────────────────────────
    public Task SendThankYouAsync(string guestEmail, string guestName, string bookingCode)
    {
        var subject = "Thank you for staying at Mouzie Guest House";
        var body = $@"
<p>Hi {guestName},</p>
<p>Thank you for staying with us. We hope you enjoyed your stay and would love to welcome you again.</p>
<p>Your booking reference was <strong>{bookingCode}</strong>.</p>
<p>— Mouzie Guest House</p>";
        return SendAsync(guestEmail, guestName, subject, body);
    }

    // ── Notify admin of a new booking ─────────────────────────────
    public Task NotifyAdminNewBookingAsync(string bookingCode, string guestName, string guestPhone,
                                           string guestIdNumber, DateTime bookingDate, string roomNumber)
    {
        var subject = $"New booking — {bookingCode}";
        var body = $@"
<p>A new booking has been submitted.</p>
<table style='border-collapse:collapse;margin:12px 0'>
  <tr><td style='padding:4px 12px 4px 0'><strong>Code</strong></td><td>{bookingCode}</td></tr>
  <tr><td style='padding:4px 12px 4px 0'><strong>Guest</strong></td><td>{guestName}</td></tr>
  <tr><td style='padding:4px 12px 4px 0'><strong>Phone</strong></td><td>{guestPhone}</td></tr>
  <tr><td style='padding:4px 12px 4px 0'><strong>SA ID</strong></td><td>{guestIdNumber}</td></tr>
  <tr><td style='padding:4px 12px 4px 0'><strong>Room</strong></td><td>Room {roomNumber}</td></tr>
  <tr><td style='padding:4px 12px 4px 0'><strong>Date</strong></td><td>{bookingDate:dd MMM yyyy}</td></tr>
</table>
<p><a href='{_site.PublicBaseUrl}/Admin/Bookings'>Open the admin dashboard</a> to approve or reject.</p>";
        return SendAsync(_site.AdminEmail, "Mouzie Admin", subject, body);
    }

    private static string WrapHtml(string inner) => $@"
<!doctype html>
<html><body style='font-family:Segoe UI,Arial,sans-serif;background:#fbf9f4;padding:24px;color:#1f2937'>
  <div style='max-width:560px;margin:0 auto;background:#ffffff;border-radius:8px;padding:32px;
              border-top:4px solid #b8942e'>
    <h2 style='color:#1e3a2f;margin-top:0'>Mouzie Guest House</h2>
    {inner}
    <hr style='border:none;border-top:1px solid #e5e7eb;margin:24px 0'/>
    <p style='font-size:12px;color:#6b7280'>
      Mouzie Guest House · Zone 13, No. 20 Langa, Cape Town<br/>
      This is an automated email. Please do not reply directly.
    </p>
  </div>
</body></html>";
}