using System;

namespace JobMatching.Domain.Entities;
public class PaymentRecord
{
    public int Id { get; set; }
    public required string UserId { get; set; }
    public User User { get; set; }
    
    public string PaymentProvider { get; set; } = string.Empty;  // Mpesa, Stripe
    public decimal Amount { get; set; }
    public string Status { get; set; } = "Pending";  // Pending, Completed, Failed
    public string TransactionId { get; set; } = string.Empty;
    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
}
