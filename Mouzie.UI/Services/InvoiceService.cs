using Mouzie.Data.Models.Domain;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Mouzie.UI.Services;

public class InvoiceService : IInvoiceService
{
    private const string Gold = "#b8942e";
    private const string DarkGreen = "#1e3a2f";
    private const string OffWhite = "#fbf9f4";

    public InvoiceService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] GenerateDamageInvoicePdf(DamageRecord damage, Booking booking, IReadOnlyDictionary<string, string> settings)
    {
        string Get(string key, string fallback = "") =>
            settings.TryGetValue(key, out var v) ? v : fallback;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(11).FontColor(Colors.Black));

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("MOUZIE GUEST HOUSE").FontSize(20).Bold().FontColor(DarkGreen);
                            c.Item().Text(Get("Address", "Zone 13, No. 20 Langa, Cape Town")).FontSize(9).FontColor(Colors.Grey.Darken1);
                            c.Item().Text($"WhatsApp: {Get("WhatsAppNumber")}").FontSize(9).FontColor(Colors.Grey.Darken1);
                        });
                        row.ConstantItem(200).AlignRight().Column(c =>
                        {
                            c.Item().Text("DAMAGE INVOICE").FontSize(22).Bold().FontColor(Gold);
                            c.Item().Text(booking.GuestName).FontSize(12).SemiBold().FontColor(DarkGreen);
                            c.Item().Text($"# DMG-{booking.BookingCode}").FontSize(10).SemiBold();
                            c.Item().Text($"Issued: {DateTime.Now:dd MMM yyyy}").FontSize(9);
                        });
                    });
                    col.Item().PaddingTop(12).LineHorizontal(1).LineColor(Gold);
                });

                page.Content().PaddingTop(16).Column(col =>
                {
                    col.Spacing(14);

                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("BILL TO").FontSize(9).Bold().FontColor(Colors.Grey.Darken2);
                            c.Item().Text(booking.GuestName).SemiBold();
                            c.Item().Text(booking.GuestEmail).FontSize(9);
                            c.Item().Text($"Tel: {booking.GuestPhone}").FontSize(9);
                        });
                        row.ConstantItem(20);
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("STAY DETAILS").FontSize(9).Bold().FontColor(Colors.Grey.Darken2);
                            c.Item().Text($"Booking code: {booking.BookingCode}").FontSize(10);
                            c.Item().Text($"Room {booking.Room?.RoomNumber ?? booking.RoomId.ToString()}").FontSize(10);
                            c.Item().Text($"Stay Date: {booking.BookingDate:dd MMM yyyy}").FontSize(10);
                        });
                    });

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(4);
                            cols.ConstantColumn(90);
                        });

                        table.Header(h =>
                        {
                            h.Cell().Background(OffWhite).Padding(6).Text("Description").Bold().FontSize(9);
                            h.Cell().Background(OffWhite).Padding(6).AlignRight().Text("Amount (R)").Bold().FontSize(9);
                        });

                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                            .Padding(6).Text(damage.Description).FontSize(10);
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                            .Padding(6).AlignRight().Text(damage.PenaltyAmount.ToString("0.00")).FontSize(10);

                        table.Cell().Padding(6).Text("TOTAL").Bold().FontColor(DarkGreen);
                        table.Cell().Padding(6).AlignRight().Text($"R {damage.PenaltyAmount:0.00}")
                            .Bold().FontSize(12).FontColor(DarkGreen);
                    });

                    col.Item().PaddingTop(18).Background(OffWhite).Padding(14).Column(c =>
                    {
                        c.Item().Text("PAYMENT — EFT ONLY").Bold().FontColor(DarkGreen).FontSize(10);
                        c.Item().Text($"Bank: {Get("BankName")}").FontSize(10);
                        c.Item().Text($"Account holder: {Get("AccountHolder")}").FontSize(10);
                        c.Item().Text($"Account number: {Get("AccountNumber")}").FontSize(10);
                        c.Item().Text($"Branch code: {Get("BranchCode")}").FontSize(10);
                        c.Item().Text($"Reference: DMG-{booking.BookingCode}").FontSize(10).SemiBold();
                    });
                });

                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span("Mouzie Guest House · ").FontSize(8).FontColor(Colors.Grey.Medium);
                    t.Span($"Damage Invoice · {booking.BookingCode}").FontSize(8).FontColor(Colors.Grey.Medium);
                });
            });
        }).GeneratePdf();
    }

    public byte[] GenerateInvoicePdf(Invoice invoice, Booking booking,
                                     IReadOnlyDictionary<string, string> settings)
    {
        string Get(string key, string fallback = "") =>
            settings.TryGetValue(key, out var v) ? v : fallback;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(11).FontColor(Colors.Black));

                // ── Header ────────────────────────────────────────────────
                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("MOUZIE GUEST HOUSE").FontSize(20).Bold().FontColor(DarkGreen);
                            c.Item().Text(Get("Address", "Zone 13, No. 20 Langa, Cape Town")).FontSize(9).FontColor(Colors.Grey.Darken1);
                            c.Item().Text($"WhatsApp: {Get("WhatsAppNumber")}").FontSize(9).FontColor(Colors.Grey.Darken1);
                        });
                        row.ConstantItem(180).AlignRight().Column(c =>
                        {
                            c.Item().Text("INVOICE").FontSize(24).Bold().FontColor(Gold);
                            c.Item().Text($"# {invoice.InvoiceNumber}").FontSize(11).SemiBold();
                            c.Item().Text($"Issued: {invoice.IssueDate:dd MMM yyyy}").FontSize(9);
                            c.Item().Text($"Due: {invoice.DueDate:dd MMM yyyy}").FontSize(9);
                        });
                    });
                    col.Item().PaddingTop(12).LineHorizontal(1).LineColor(Gold);
                });

                // ── Content ───────────────────────────────────────────────
                page.Content().PaddingTop(16).Column(col =>
                {
                    col.Spacing(14);

                    // Bill to / Stay
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("BILL TO").FontSize(9).Bold().FontColor(Colors.Grey.Darken2);
                            c.Item().Text(booking.GuestName).SemiBold();
                            c.Item().Text(booking.GuestEmail).FontSize(9);
                            c.Item().Text($"Tel: {booking.GuestPhone}").FontSize(9);
                            if (!booking.GuestIsForeign)
                                c.Item().Text($"SA ID: {MaskId(booking.GuestIdNumber)}").FontSize(9);
                        });
                        row.ConstantItem(20);
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("STAY DETAILS").FontSize(9).Bold().FontColor(Colors.Grey.Darken2);
                            c.Item().Text($"Booking code: {booking.BookingCode}").FontSize(10);
                            c.Item().Text($"Room {booking.Room?.RoomNumber ?? "—"}").FontSize(10);
                            c.Item().Text($"Date: {booking.BookingDate:dd MMM yyyy}").FontSize(10);
                            c.Item().Text($"Check-in {Get("CheckInTime", "14:00")} · Check-out {Get("CheckOutTime", "10:00")}").FontSize(9);
                        });
                    });

                    // Items table
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(4);
                            cols.ConstantColumn(90);
                        });

                        table.Header(h =>
                        {
                            h.Cell().Background(OffWhite).Padding(6).Text("Description").Bold().FontSize(9);
                            h.Cell().Background(OffWhite).Padding(6).AlignRight().Text("Amount (R)").Bold().FontSize(9);
                        });

                        foreach (var item in invoice.Items)
                        {
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                                .Padding(6).Text(item.Description).FontSize(10);
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                                .Padding(6).AlignRight().Text(item.Amount.ToString("0.00")).FontSize(10);
                        }

                        table.Cell().Padding(6).Text("TOTAL").Bold().FontColor(DarkGreen);
                        table.Cell().Padding(6).AlignRight().Text($"R {invoice.TotalAmount:0.00}")
                            .Bold().FontSize(12).FontColor(DarkGreen);
                    });

                    // Banking details
                    col.Item().PaddingTop(18).Background(OffWhite).Padding(14).Column(c =>
                    {
                        c.Item().Text("PAYMENT — EFT ONLY").Bold().FontColor(DarkGreen).FontSize(10);
                        c.Item().Text($"Bank: {Get("BankName")}").FontSize(10);
                        c.Item().Text($"Account holder: {Get("AccountHolder")}").FontSize(10);
                        c.Item().Text($"Account number: {Get("AccountNumber")}").FontSize(10);
                        c.Item().Text($"Branch code: {Get("BranchCode")}").FontSize(10);
                        c.Item().Text($"Reference: {invoice.InvoiceNumber} / {booking.BookingCode}").FontSize(10).SemiBold();
                    });

                    col.Item().PaddingTop(8).Text(
                        "Thank you for choosing Mouzie Guest House. Your comfort is our priority.")
                        .Italic().FontSize(9).FontColor(Colors.Grey.Darken1);
                });

                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span("Mouzie Guest House · ").FontSize(8).FontColor(Colors.Grey.Medium);
                    t.Span($"Invoice {invoice.InvoiceNumber}").FontSize(8).FontColor(Colors.Grey.Medium);
                });
            });
        }).GeneratePdf();
    }

    public byte[] GenerateDailyLogPdf(DateTime date, IEnumerable<Booking> bookings)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(col =>
                {
                    col.Item().Text($"Daily Check-in / Check-out Log — {date:dd MMM yyyy}")
                        .FontSize(16).Bold().FontColor(DarkGreen);
                    col.Item().PaddingTop(4).LineHorizontal(1).LineColor(Gold);
                });

                page.Content().PaddingTop(10).Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.ConstantColumn(60);   // Room
                        cols.ConstantColumn(70);   // Code
                        cols.RelativeColumn(2);    // Guest
                        cols.RelativeColumn(2);    // Email
                        cols.ConstantColumn(90);   // Phone
                        cols.ConstantColumn(100);  // SA ID
                        cols.ConstantColumn(90);   // Check-in
                        cols.ConstantColumn(90);   // Check-out
                        cols.ConstantColumn(90);   // Status
                    });

                    table.Header(h =>
                    {
                        foreach (var heading in new[] { "Room", "Code", "Guest", "Email", "Phone", "SA ID", "Check-in", "Check-out", "Status" })
                            h.Cell().Background(OffWhite).Padding(4).Text(heading).Bold().FontSize(8);
                    });

                    foreach (var b in bookings)
                    {
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(b.Room?.RoomNumber ?? "—").FontSize(9);
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(b.BookingCode).FontSize(9);
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(b.GuestName).FontSize(9);
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(b.GuestEmail).FontSize(8);
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(b.GuestPhone).FontSize(9);
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(MaskId(b.GuestIdNumber)).FontSize(8);
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(b.CheckInTime?.ToString("HH:mm") ?? "—").FontSize(9);
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(b.CheckOutTime?.ToString("HH:mm") ?? "—").FontSize(9);
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(b.Status).FontSize(9);
                    }
                });
            });
        }).GeneratePdf();
    }

    // POPIA-friendly masking: show first 6 + last 2, mask middle 5
    private static string MaskId(string id) =>
        string.IsNullOrWhiteSpace(id) || id.Length != 13
            ? "—"
            : $"{id[..6]}*****{id[^2..]}";
}