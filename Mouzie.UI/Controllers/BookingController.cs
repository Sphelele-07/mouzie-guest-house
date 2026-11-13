using Microsoft.AspNetCore.Mvc;
using Mouzie.Data.Models.Domain;
using Mouzie.Data.Models.ViewModels;
using Mouzie.Data.Repositories.Interfaces;
using Mouzie.UI.Services;
using Microsoft.AspNetCore.RateLimiting;

namespace Mouzie.UI.Controllers;

public class BookingController : Controller
{
    private readonly IRoomRepository _rooms;
    private readonly IBookingRepository _bookings;
    private readonly ISiteSettingRepository _settings;
    private readonly IEmailService _email;
    private readonly IBookingCodeGenerator _codes;
    private readonly ILogger<BookingController> _log;

    public BookingController(IRoomRepository rooms, IBookingRepository bookings,
                             ISiteSettingRepository settings, IEmailService email,
                             IBookingCodeGenerator codes, ILogger<BookingController> log)
    {
        _rooms = rooms;
        _bookings = bookings;
        _settings = settings;
        _email = email;
        _codes = codes;
        _log = log;
    }

    // ══════════════════════════════════════════════════════════════
    // GET: /Booking/Create?roomId=1
    // ══════════════════════════════════════════════════════════════
    [HttpGet]
    public async Task<IActionResult> Create(int? roomId)
    {
        var rooms = (await _rooms.GetAllAsync()).ToList();
        if (rooms.Count == 0)
        {
            TempData["Error"] = "No rooms available right now. Please try again later.";
            return RedirectToAction("Index", "Home");
        }

        var vm = new BookingRequestVM
        {
            RoomId = roomId ?? rooms[0].RoomId,
            BookingDate = DateTime.Today.AddDays(1)
        };

        ViewData["Rooms"] = rooms;
        ViewData["AvailableDates"] = await GetAvailableDatesForRoomAsync(vm.RoomId);
        ViewData["Title"] = "Book a Room";
        return View(vm);
    }

    // ══════════════════════════════════════════════════════════════
    // POST: /Booking/Create
    // ══════════════════════════════════════════════════════════════
    [HttpPost, ValidateAntiForgeryToken]
    [EnableRateLimiting("booking")]
    public async Task<IActionResult> Create(BookingRequestVM vm)
    {
        var rooms = (await _rooms.GetAllAsync()).ToList();
        ViewData["Rooms"] = rooms;
        ViewData["AvailableDates"] = await GetAvailableDatesForRoomAsync(vm.RoomId);

        if (!ModelState.IsValid)
            return View(vm);

        // ── Foreign national override ────────────────────────────
        if (vm.GuestIsForeign)
        {
            if (string.IsNullOrWhiteSpace(vm.GuestIdNumber) ||
                vm.GuestIdNumber.Length < 6 || vm.GuestIdNumber.Length > 15)
            {
                ModelState.AddModelError(nameof(vm.GuestIdNumber),
                    "Passport number must be 6–15 characters.");
                return View(vm);
            }
        }

        // ── Duplicate check: same SA ID, same date ───────────────
        var sameGuest = await _bookings.GetByGuestIdNumberAsync(vm.GuestIdNumber);
        var duplicate = sameGuest.FirstOrDefault(b =>
            b.BookingDate.Date == vm.BookingDate.Date &&
            b.Status != "Rejected" && b.Status != "Cancelled");

        if (duplicate is not null)
        {
            ModelState.AddModelError(string.Empty,
                $"You already have a booking on {vm.BookingDate:dd MMM yyyy} " +
                $"(code {duplicate.BookingCode}). Please WhatsApp us if you need to change it.");
            return View(vm);
        }

        // ── Room availability ────────────────────────────────────
        var clash = (await _bookings.GetByRoomAndDateAsync(vm.RoomId, vm.BookingDate)).ToList();
        if (clash.Any(b => b.Status == "Approved" || b.Status == "CheckedIn"))
        {
            ModelState.AddModelError(string.Empty,
                "Sorry, that room is already booked for the selected date. " +
                "Please pick another date or room.");
            return View(vm);
        }

        // ── Generate booking code ────────────────────────────────
        var code = await _codes.GenerateUniqueAsync();

        var booking = new Booking
        {
            RoomId = vm.RoomId,
            GuestName = vm.GuestName.Trim(),
            GuestEmail = vm.GuestEmail.Trim().ToLowerInvariant(),
            GuestPhone = vm.GuestPhone.Trim(),
            GuestIdNumber = vm.GuestIdNumber.Trim(),
            GuestIsForeign = vm.GuestIsForeign,
            BookingDate = vm.BookingDate.Date,
            Status = "Pending",
            BookingCode = code
        };

        await _bookings.CreateBookingAsync(booking);

        // ── Send emails (best-effort) ────────────────────────────
        try
        {
            var room = rooms.First(r => r.RoomId == vm.RoomId);

            await _email.SendBookingReceivedAsync(
                vm.GuestEmail, vm.GuestName, code,
                vm.BookingDate, room.RoomNumber, room.PricePerNight);

            await _email.NotifyAdminNewBookingAsync(
                code, vm.GuestName, vm.GuestPhone, vm.GuestIdNumber,
                vm.BookingDate, room.RoomNumber);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Email failed for booking {Code}", code);
        }

        return RedirectToAction(nameof(Success), new { code });
    }

    private static string FormatName(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;

        // Split into words (by space, hyphen, or apostrophe)
        var words = input.Trim().ToLowerInvariant().Split(new[] { ' ', '-', '\'' }, StringSplitOptions.RemoveEmptyEntries);

        // Capitalize the first letter of each word and keep the separators
        var result = new System.Text.StringBuilder();
        int wordIndex = 0;

        foreach (char c in input.Trim().ToLowerInvariant())
        {
            if (c == ' ' || c == '-' || c == '\'')
            {
                result.Append(c);
                wordIndex = 0; // Reset for next word
            }
            else
            {
                result.Append(wordIndex == 0 ? char.ToUpperInvariant(c) : c);
                wordIndex++;
            }
        }

        return result.ToString();
    }

    // ── GET /Booking/Verify ────────────────────────────────────────
    [HttpGet]
    public IActionResult Verify()
    {
        ViewData["Title"] = "Verify Booking";
        return View((Mouzie.Data.Models.Domain.Booking?)null);
    }

    // ── POST /Booking/Verify ───────────────────────────────────────
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Verify(string bookingCode)
    {
        if (string.IsNullOrWhiteSpace(bookingCode))
        {
            ModelState.AddModelError(string.Empty, "Please enter a booking code.");
            return View((Mouzie.Data.Models.Domain.Booking?)null);
        }

        var booking = await _bookings.GetByCodeAsync(bookingCode.Trim().ToUpperInvariant());

        if (booking == null)
        {
            ModelState.AddModelError(string.Empty, "No booking found with that code. Please check the code and try again.");
            return View((Mouzie.Data.Models.Domain.Booking?)null);
        }

        // Ensure room details are loaded
        if (booking.Room is null && booking.RoomId > 0)
        {
            booking.Room = await _rooms.GetByIdAsync(booking.RoomId);
        }

        return View(booking);
    }

    // Add this helper method inside your BookingController class
    private DateTime GetSaTime()
    {
        // South Africa Standard Time (SAST) is UTC+2
        // Using the Windows/IANA timezone ID for South Africa
        var saZone = TimeZoneInfo.FindSystemTimeZoneById("South Africa Standard Time");
        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, saZone);
    }

    // ── GET /Booking/Success/{code} ────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> Success(string code)
    {
        var booking = await _bookings.GetByCodeAsync(code);
        if (booking is null) return NotFound();

        // ✅ Manually load the room if not joined by the SP
        if (booking.Room is null && booking.RoomId > 0)
        {
            booking.Room = await _rooms.GetByIdAsync(booking.RoomId);
        }

        var settings = (await _settings.GetAllSettingsAsync())
                       .ToDictionary(s => s.SettingKey, s => s.SettingValue);

        ViewData["Title"] = "Booking received";
        ViewBag.WhatsAppNumber = settings.TryGetValue("WhatsAppNumber", out var w) ? w : "";
        return View(booking);
    }

    // ── AJAX helper: available dates for a room ────────────────────
    [HttpGet]
    public async Task<IActionResult> AvailableDates(int roomId)
    {
        var dates = await GetAvailableDatesForRoomAsync(roomId);
        return Json(dates.Select(d => d.ToString("yyyy-MM-dd")));
    }

    private async Task<List<DateTime>> GetAvailableDatesForRoomAsync(int roomId)
    {
        var today = DateTime.Today;
        var result = new List<DateTime>();

        // Check if room is completely blocked right now
        var room = await _rooms.GetByIdAsync(roomId);
        bool isBlockedToday = room != null && (room.Status == "Occupied" || room.Status == "Maintenance");

        for (int i = 0; i < 30; i++)
        {
            var day = today.AddDays(i);

            // If it's today and the room is blocked, skip it
            if (i == 0 && isBlockedToday) continue;

            var existing = await _bookings.GetByRoomAndDateAsync(roomId, day);
            var taken = existing.Any(b => b.Status == "Approved" || b.Status == "CheckedIn");

            if (!taken) result.Add(day);
        }
        return result;
    }
}