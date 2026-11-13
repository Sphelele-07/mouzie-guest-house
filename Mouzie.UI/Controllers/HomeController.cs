using Microsoft.AspNetCore.Mvc;
using Mouzie.Data.Models.ViewModels;
using Mouzie.Data.Repositories.Interfaces;

namespace Mouzie.UI.Controllers;

public class HomeController : Controller
{
    private readonly IRoomRepository _rooms;
    private readonly ISiteSettingRepository _settings;

    public HomeController(IRoomRepository rooms, ISiteSettingRepository settings)
    {
        _rooms = rooms;
        _settings = settings;
    }

    public async Task<IActionResult> Index()
    {
        var settings = (await _settings.GetAllSettingsAsync())
                       .ToDictionary(s => s.SettingKey, s => s.SettingValue);

        var vm = new HomeVM
        {
            Rooms = (await _rooms.GetAllAsync()).ToList(),
            WhatsAppNumber = Get(settings, "WhatsAppNumber", "+27821234567"),
            Address = Get(settings, "Address", "Zone 13, No. 20 Langa, Cape Town"),
            BusinessName = Get(settings, "BusinessName", "Mouzie Guest House"),
            Tagline = "Your comfort is our priority.",
            CheckInTime = Get(settings, "CheckInTime", "14:00"),
            CheckOutTime = Get(settings, "CheckOutTime", "10:00")
        };

        ViewData["Title"] = "Home";
        return View(vm);
    }

    public IActionResult About() { ViewData["Title"] = "About"; return View(); }
    public IActionResult Contact() { ViewData["Title"] = "Contact"; return View(); }
    public IActionResult Visit() { ViewData["Title"] = "Visit Us"; return View(); }
    public IActionResult Facilities() { ViewData["Title"] = "Facilities"; return View(); }
    public IActionResult Terms() { ViewData["Title"] = "Terms & Conditions"; return View(); }

    public IActionResult Rooms() => RedirectToAction(nameof(Index));
    public IActionResult Error() => Content("Something went wrong. Please try again.");

    private static string Get(IReadOnlyDictionary<string, string> d, string k, string fallback)
        => d.TryGetValue(k, out var v) && !string.IsNullOrWhiteSpace(v) ? v : fallback;
}