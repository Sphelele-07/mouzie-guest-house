using Microsoft.AspNetCore.Mvc;
using Mouzie.Data.Models.Domain;
using Mouzie.Data.Models.ViewModels;
using Mouzie.Data.Repositories.Interfaces;
using Mouzie.UI.Filters;
using Mouzie.UI.Services;
using System.Text.RegularExpressions;

namespace Mouzie.UI.Controllers;

[AdminAuth]
public class AdminController : Controller
{
    private readonly IBookingRepository _bookings;
    private readonly IRoomRepository _rooms;
    private readonly IInvoiceRepository _invoices;
    private readonly ISiteSettingRepository _settings;
    private readonly IEmailService _email;
    private readonly IInvoiceService _pdf;
    private readonly IDamageRepository _damages;
    private readonly IUserRepository _users;
    private readonly ISessionService _session;

    public AdminController(
        IBookingRepository bookings,
        IRoomRepository rooms,
        IInvoiceRepository invoices,
        ISiteSettingRepository settings,
        IEmailService email,
        IInvoiceService pdf,
        IDamageRepository damages,
        IUserRepository users,
        ISessionService session)
    {
        _bookings = bookings;
        _rooms = rooms;
        _invoices = invoices;
        _settings = settings;
        _email = email;
        _pdf = pdf;
        _damages = damages;
        _users = users;
        _session = session;
    }

    // ── Helpers ─────────────────────────────────────────────────────
    private DateTime GetSaTime(DateTime? utc)
    {
        if (!utc.HasValue) return DateTime.Now;
        try
        {
            var saZone = TimeZoneInfo.FindSystemTimeZoneById("South Africa Standard Time");
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc.Value, DateTimeKind.Utc), saZone);
        }
        catch
        {
            try
            {
                var saZone = TimeZoneInfo.FindSystemTimeZoneById("Africa/Johannesburg");
                return TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc.Value, DateTimeKind.Utc), saZone);
            }
            catch
            {
                return utc.Value.AddHours(2);
            }
        }
    }

    private async Task<Dictionary<string, string>> GetSettingsAsync()
        => (await _settings.GetAllSettingsAsync()).ToDictionary(s => s.SettingKey, s => s.SettingValue);

    // ── DASHBOARD ──────────────────────────────────────────────────
    public async Task<IActionResult> Index(string period = "6months")
    {
        var all = (await _bookings.GetAllAsync()).ToList();
        var today = DateTime.Today;

        var vm = new AdminDashboardVM
        {
            TotalBookings = all.Count,
            PendingBookings = all.Count(b => b.Status == "Pending"),
            RoomsOccupied = all.Count(b => b.Status == "CheckedIn"),
            TotalRooms = (await _rooms.GetAllAsync()).Count(),
            MonthlyRevenue = all.Where(b => b.Status == "CheckedOut"
                                && b.CheckOutTime.HasValue
                                && GetSaTime(b.CheckOutTime.Value).Month == today.Month
                                && GetSaTime(b.CheckOutTime.Value).Year == today.Year)
                             .Sum(b => b.Room?.PricePerNight ?? 600m),
            TodayCheckIns = all.Count(b => b.CheckInTime.HasValue && GetSaTime(b.CheckInTime.Value).Date == today),
            TodayCheckOuts = all.Count(b => b.CheckOutTime.HasValue && GetSaTime(b.CheckOutTime.Value).Date == today),
            RecentBookings = all.OrderByDescending(b => b.CreatedAt).Take(5).ToList(),
            SelectedPeriod = period
        };

        int monthsToShow = period switch
        {
            "3months" => 3,
            "6months" => 6,
            "12months" => 12,
            "thisyear" => DateTime.Today.Month,
            _ => 6
        };

        for (int i = monthsToShow - 1; i >= 0; i--)
        {
            var monthDate = today.AddMonths(-i);
            var monthStart = new DateTime(monthDate.Year, monthDate.Month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);

            var revenue = all
                .Where(b => b.Status == "CheckedOut"
                         && b.CheckOutTime.HasValue
                         && GetSaTime(b.CheckOutTime.Value) >= monthStart
                         && GetSaTime(b.CheckOutTime.Value) <= monthEnd)
                .Sum(b => b.Room?.PricePerNight ?? 600m);

            vm.ChartLabels.Add(monthDate.ToString("MMM yyyy"));
            vm.ChartData.Add(revenue);
        }

        ViewData["Title"] = "Dashboard";
        return View(vm);
    }

    // ── BOOKINGS LIST ──────────────────────────────────────────────
    public async Task<IActionResult> Bookings(string? status = null)
    {
        var bookings = (await _bookings.GetAllAsync(status)).ToList();
        ViewData["Title"] = "Bookings";
        ViewData["CurrentStatus"] = status;
        return View(bookings);
    }

    // ── APPROVE ─────────────────────────────────────────────────────
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id)
    {
        var booking = await _bookings.GetByIdAsync(id);
        if (booking == null) return NotFound();

        if (booking.Room == null && booking.RoomId > 0)
            booking.Room = await _rooms.GetByIdAsync(booking.RoomId);

        await _bookings.UpdateStatusAsync(id, "Approved", GetSaTime(DateTime.UtcNow));

        var nameParts = booking.GuestName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var surname = nameParts.Length > 1 ? nameParts[^1] : nameParts[0];
        var cleanSurname = Regex.Replace(surname, @"[^a-zA-Z]", "");

        var settings = await GetSettingsAsync();
        var invoice = new Invoice
        {
            BookingId = id,
            InvoiceNumber = $"INV-{cleanSurname}-{booking.BookingCode}",
            IssueDate = GetSaTime(DateTime.UtcNow),
            DueDate = GetSaTime(DateTime.UtcNow).AddDays(3),
            Status = "Unpaid",
            TotalAmount = booking.Room?.PricePerNight ?? 600m,
            Items = new List<InvoiceItem>
            {
                new InvoiceItem
                {
                    Description = $"Room {booking.Room?.RoomNumber ?? booking.RoomId.ToString()} — {booking.BookingDate:dd MMM yyyy}",
                    Amount = booking.Room?.PricePerNight ?? 600m
                }
            }
        };

        await _invoices.CreateInvoiceAsync(invoice);
        var fullInvoice = await _invoices.GetByBookingIdAsync(id);

        if (fullInvoice != null)
        {
            try
            {
                var pdfBytes = _pdf.GenerateInvoicePdf(fullInvoice, booking, settings);
                await _email.SendBookingApprovedAsync(
                    booking.GuestEmail, booking.GuestName, booking.BookingCode,
                    pdfBytes, fullInvoice.InvoiceNumber);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Email failed: {ex.Message}");
            }
        }

        TempData["Success"] = $"Booking {booking.BookingCode} approved and invoice sent.";
        return RedirectToAction(nameof(Bookings));
    }

    // ── DOWNLOAD BOOKING INVOICE ────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> DownloadInvoice(int id)
    {
        var booking = await _bookings.GetByIdAsync(id);
        if (booking == null) return NotFound();

        var invoice = await _invoices.GetByBookingIdAsync(id);
        if (invoice == null) return NotFound("Invoice not generated yet.");

        if (booking.Room == null)
            booking.Room = await _rooms.GetByIdAsync(booking.RoomId);

        var settings = await GetSettingsAsync();
        var pdfBytes = _pdf.GenerateInvoicePdf(invoice, booking, settings);

        var safeGuestName = Regex.Replace(booking.GuestName, @"[^a-zA-Z ]", "").Replace(" ", "_");
        var fileName = $"{invoice.InvoiceNumber}_{safeGuestName}.pdf";

        return File(pdfBytes, "application/pdf", fileName);
    }

    // ── DOWNLOAD DAMAGE INVOICE ─────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> DownloadDamageInvoice(int id)
    {
        var damage = await _damages.GetByIdAsync(id);
        if (damage == null) return NotFound($"Damage record {id} not found.");

        var booking = await _bookings.GetByIdAsync(damage.BookingId);
        if (booking == null) return NotFound($"Booking {damage.BookingId} not found.");

        if (booking.Room == null)
            booking.Room = await _rooms.GetByIdAsync(booking.RoomId);

        var settings = await GetSettingsAsync();
        var pdfBytes = _pdf.GenerateDamageInvoicePdf(damage, booking, settings);

        // Send email to the guest
        try
        {
            var subject = $"Damage Invoice — Booking {booking.BookingCode}";
            var body = $@"
                <p>Dear {booking.GuestName},</p>
                <p>Please find attached an invoice for damages reported during your recent stay at Mouzie Guest House.</p>
                <p><strong>Description:</strong> {damage.Description}</p>
                <p><strong>Amount Due:</strong> R {damage.PenaltyAmount:0.00}</p>
                <p>Payment is via EFT using the banking details on the invoice.</p>
                <p>Thank you,<br/>Mouzie Guest House</p>";

            await _email.SendAsync(
                booking.GuestEmail,
                booking.GuestName,
                subject,
                body,
                new[] { new EmailAttachment($"Damage-{booking.BookingCode}.pdf", pdfBytes) });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Damage invoice email failed: {ex.Message}");
        }

        var safeGuestName = Regex.Replace(booking.GuestName, @"[^a-zA-Z ]", "").Replace(" ", "_");
        var fileName = $"Damage-{booking.BookingCode}_{safeGuestName}.pdf";
        return File(pdfBytes, "application/pdf", fileName);
    }

    // ── REJECT ──────────────────────────────────────────────────────
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id)
    {
        var booking = await _bookings.GetByIdAsync(id);
        if (booking == null) return NotFound();

        await _bookings.UpdateStatusAsync(id, "Rejected", GetSaTime(DateTime.UtcNow));
        TempData["Success"] = $"Booking {booking.BookingCode} rejected.";
        return RedirectToAction(nameof(Bookings));
    }

    // ── CHECK-IN ────────────────────────────────────────────────────
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CheckIn(int id)
    {
        var booking = await _bookings.GetByIdAsync(id);
        if (booking == null) return NotFound();

        // Store UTC in DB to prevent double-shifting
        await _bookings.SetCheckInAsync(id, DateTime.UtcNow);
        TempData["Success"] = $"{booking.GuestName} checked in.";
        return RedirectToAction(nameof(Bookings));
    }

    // ── CHECK-OUT ───────────────────────────────────────────────────
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CheckOut(int id, bool isEarly = false, string? reason = null,
        string? damageDescription = null, decimal damageAmount = 0, string? damageSeverity = null)
    {
        var booking = await _bookings.GetByIdAsync(id);
        if (booking == null) return NotFound();

        // Store UTC in DB to prevent double-shifting
        await _bookings.SetCheckOutAsync(id, DateTime.UtcNow, isEarly, reason);

        // Save damage if reported
        if (!string.IsNullOrWhiteSpace(damageDescription) && damageAmount > 0)
        {
            await _damages.InsertAsync(new DamageRecord
            {
                BookingId = id,
                Description = damageDescription,
                PenaltyAmount = damageAmount,
                Severity = damageSeverity ?? "Minor"
            });
        }

        try
        {
            await _email.SendThankYouAsync(booking.GuestEmail, booking.GuestName, booking.BookingCode);
        }
        catch { }

        TempData["Success"] = $"{booking.GuestName} checked out. Room is now available.";
        return RedirectToAction(nameof(Bookings));
    }

    // ── ROOMS ───────────────────────────────────────────────────────
    public async Task<IActionResult> Rooms()
    {
        var rooms = await _rooms.GetAllAsync(includeInactive: true);
        ViewData["Title"] = "Rooms";
        return View(rooms);
    }

    [HttpGet]
    public async Task<IActionResult> RoomEdit(int? id)
    {
        var vm = new RoomEditVM();
        if (id.HasValue)
        {
            var room = await _rooms.GetByIdAsync(id.Value);
            if (room == null) return NotFound();
            vm.RoomId = room.RoomId;
            vm.RoomNumber = room.RoomNumber;
            vm.RoomType = room.RoomType;
            vm.PricePerNight = room.PricePerNight;
            vm.Status = room.Status;
            vm.RoomKeyCode = room.RoomKeyCode;
            vm.ImagePath = room.ImagePath;
        }
        ViewData["Title"] = id.HasValue ? "Edit Room" : "Add Room";
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RoomEdit(RoomEditVM vm)
    {
        if (!ModelState.IsValid) return View(vm);

        if (vm.RoomId == 0)
        {
            var room = new Room
            {
                RoomNumber = vm.RoomNumber,
                RoomType = vm.RoomType,
                PricePerNight = vm.PricePerNight,
                Status = vm.Status,
                RoomKeyCode = vm.RoomKeyCode,
                ImagePath = vm.ImagePath
            };
            await _rooms.InsertAsync(room);
            TempData["Success"] = "Room added.";
        }
        else
        {
            var room = await _rooms.GetByIdAsync(vm.RoomId);
            if (room == null) return NotFound();

            room.RoomNumber = vm.RoomNumber;
            room.RoomType = vm.RoomType;
            room.PricePerNight = vm.PricePerNight;
            room.Status = vm.Status;
            room.RoomKeyCode = vm.RoomKeyCode;
            room.ImagePath = vm.ImagePath;
            await _rooms.UpdateAsync(room);
            TempData["Success"] = "Room updated.";
        }

        return RedirectToAction(nameof(Rooms));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RoomDelete(int id)
    {
        await _rooms.SoftDeleteAsync(id);
        TempData["Success"] = "Room removed from the list.";
        return RedirectToAction(nameof(Rooms));
    }

    // ── SETTINGS ────────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> Settings()
    {
        var settings = await GetSettingsAsync();
        ViewData["Title"] = "Settings";
        return View(settings);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Settings(Dictionary<string, string> settings)
    {
        foreach (var kvp in settings)
        {
            await _settings.UpsertSettingAsync(kvp.Key, kvp.Value);
        }
        TempData["Success"] = "Settings saved.";
        return RedirectToAction(nameof(Settings));
    }

    // ── PROFILE ─────────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var user = await _users.GetByIdAsync(_session.UserId ?? 0);
        if (user == null) return NotFound();
        ViewData["Title"] = "My Profile";
        return View(user);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(User model, IFormFile? profileImage)
    {
        if (model.UserId == 0) return BadRequest();

        string? imagePath = model.ProfileImagePath;

        if (profileImage != null && profileImage.Length > 0)
        {
            var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "profiles");
            if (!Directory.Exists(uploads)) Directory.CreateDirectory(uploads);

            var fileName = $"manager-{Guid.NewGuid()}.jpg";
            var filePath = Path.Combine(uploads, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await profileImage.CopyToAsync(stream);
            }

            imagePath = $"/images/profiles/{fileName}";
        }

        await _users.UpdateProfileAsync(model.UserId, model.FirstName ?? "", model.LastName ?? "", model.Phone ?? "", imagePath ?? "");
        TempData["Success"] = "Profile updated.";
        return RedirectToAction(nameof(Profile));
    }

    // ── DAMAGES ─────────────────────────────────────────────────────
    public async Task<IActionResult> Damages()
    {
        var damages = await _damages.GetAllAsync();
        ViewData["Title"] = "Damage Records";
        return View(damages);
    }

    // ── DAILY LOG PDF ────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> DownloadDailyLog(DateTime? date)
    {
        var targetDate = date ?? DateTime.Today;
        var bookings = (await _bookings.GetAllAsync()).ToList();

        var dailyBookings = bookings
            .Where(b => (b.CheckInTime.HasValue && GetSaTime(b.CheckInTime.Value).Date == targetDate)
                     || (b.CheckOutTime.HasValue && GetSaTime(b.CheckOutTime.Value).Date == targetDate)
                     || b.BookingDate.Date == targetDate)
            .ToList();

        var pdfBytes = _pdf.GenerateDailyLogPdf(targetDate, dailyBookings);
        return File(pdfBytes, "application/pdf", $"DailyLog-{targetDate:yyyyMMdd}.pdf");
    }
}