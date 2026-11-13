using Mouzie.Data.Models.Domain;

namespace Mouzie.Data.Models.ViewModels;

public class AdminDashboardVM
{
    public int TotalBookings { get; set; }
    public int PendingBookings { get; set; }
    public int RoomsOccupied { get; set; }
    public int TotalRooms { get; set; }
    public decimal MonthlyRevenue { get; set; }
    public int TodayCheckIns { get; set; }
    public int TodayCheckOuts { get; set; }
    public List<Booking> RecentBookings { get; set; } = new();

    // 👇 NEW PROPERTIES FOR THE GRAPH
    public string SelectedPeriod { get; set; } = "6months"; // "3months", "6months", "12months", "thisyear"
    public List<string> ChartLabels { get; set; } = new();
    public List<decimal> ChartData { get; set; } = new();
}