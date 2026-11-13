using System;
using System.Collections.Generic;
using System.Text;

namespace Mouzie.Data.Models.Domain
{
    public class Invoice
    {
        public int InvoiceId { get; set; }
        public int BookingId { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateTime IssueDate { get; set; } = DateTime.UtcNow;
        public DateTime DueDate { get; set; } = DateTime.UtcNow.AddDays(3);
        public string Status { get; set; } = "Unpaid";        // Unpaid | Paid | Cancelled
        public decimal TotalAmount { get; set; }
        public bool IsDeleted { get; set; } = false;
        public List<InvoiceItem> Items { get; set; } = new();
    }

    public class InvoiceItem
    {
        public int InvoiceItemId { get; set; }
        public int InvoiceId { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public bool IsDeleted { get; set; } = false;
    }
}
